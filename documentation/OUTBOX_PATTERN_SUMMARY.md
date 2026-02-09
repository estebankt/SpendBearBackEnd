# Outbox Pattern & Analytics Rebuild - Implementation Summary

**Date:** 2026-02-09
**Status:** Implemented
**Scope:** Cross-cutting (SharedKernel, Infrastructure.Core, Spending, Analytics)

---

## Problem Statement

The Analytics module had several reliability problems:

1. **Seeded data bypassed events.** `DevelopmentDataSeeder` uses raw SQL, so domain events never fire. Analytics snapshots were pre-computed in the seeder and could diverge from real handler logic.

2. **No recovery from event handling failures.** Events were dispatched synchronously after `BaseDbContext.SaveChangesAsync()` committed. If the Analytics handler threw, the Spending transaction was already committed but analytics snapshots never updated. No retry, no logging, no rebuild mechanism.

3. **Statement import chain was fragile.** The chain is 3 levels deep (`StatementImportConfirmed -> Transaction.Create -> TransactionCreated -> Analytics`). A failure at any level after a prior DB commit left data permanently inconsistent.

---

## Solution Overview

Two complementary mechanisms were implemented:

1. **Outbox Pattern** — Domain events are written atomically alongside business data, then processed asynchronously by a background service with retry logic.
2. **Rebuild Command** — A `POST /api/analytics/rebuild` endpoint that recomputes all analytics snapshots from source transaction data.

---

## Architecture

### Event Flow (Before)

```
SaveChangesAsync()
    |-> base.SaveChanges() (commits business data)
    |-> IDomainEventDispatcher.DispatchAsync() (synchronous, inline)
         |-> Handler throws? Event lost forever.
```

### Event Flow (After)

```
SaveChangesAsync()
    |-> Collect domain events from aggregates
    |-> Clear events from aggregates
    |-> base.SaveChanges() (commits business data)
    |-> INSERT into shared.outbox_messages (same connection/transaction)
         (atomic with business data)

OutboxProcessor (BackgroundService, polls every 1s)
    |-> SELECT ... FOR UPDATE SKIP LOCKED (row-level locking)
    |-> Deserialize event -> IDomainEventDispatcher.DispatchAsync()
    |-> Success: SET processed_at = NOW()
    |-> Failure: INCREMENT retry_count, SET last_error
    |-> Dead letters: Messages exceeding max retries stay for investigation
    |-> Cleanup: Delete processed messages older than retention period
```

---

## Database Schema

```sql
CREATE SCHEMA IF NOT EXISTS shared;

CREATE TABLE shared.outbox_messages (
    id              UUID PRIMARY KEY,
    event_type      TEXT NOT NULL,           -- Assembly-qualified type name
    payload         JSONB NOT NULL,          -- JSON-serialized domain event
    occurred_on     TIMESTAMPTZ NOT NULL,    -- When the event originally occurred
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at    TIMESTAMPTZ NULL,        -- NULL = unprocessed
    retry_count     INTEGER NOT NULL DEFAULT 0,
    last_error      TEXT NULL,               -- Error from last failed attempt
    source_module   TEXT NOT NULL DEFAULT '' -- Module that generated the event
);

-- Partial index for efficient polling of unprocessed messages
CREATE INDEX ix_outbox_unprocessed ON shared.outbox_messages (created_at ASC)
    WHERE processed_at IS NULL;

-- Index for cleanup of old processed messages
CREATE INDEX ix_outbox_processed_cleanup ON shared.outbox_messages (processed_at)
    WHERE processed_at IS NOT NULL;
```

The table is created at startup by `OutboxTableInitializer.EnsureOutboxTableAsync()` (idempotent — safe to run every startup).

---

## Implementation Details

### New Files

| File | Purpose |
|------|---------|
| `Infrastructure.Core/Outbox/OutboxMessage.cs` | POCO for outbox table rows (not an EF entity) |
| `Infrastructure.Core/Outbox/OutboxTableInitializer.cs` | Static method to create schema + table via raw Npgsql |
| `Infrastructure.Core/Outbox/OutboxProcessor.cs` | `BackgroundService` that polls, dispatches, retries, and cleans up |
| `SharedKernel/ITransactionReadService.cs` | Cross-module read contract + `TransactionReadModel` record |
| `Spending.Infrastructure/Services/TransactionReadService.cs` | Implementation that streams transactions via `IAsyncEnumerable` |
| `Analytics.Application/Features/Commands/RebuildAnalytics/RebuildAnalyticsCommand.cs` | Command record with optional `UserId` |
| `Analytics.Application/Features/Commands/RebuildAnalytics/RebuildAnalyticsHandler.cs` | Deletes existing snapshots and rebuilds from transaction data |

### Modified Files

| File | Change |
|------|--------|
| `Infrastructure.Core/Data/BaseDbContext.cs` | Writes events to outbox instead of dispatching inline; removed `IDomainEventDispatcher` dependency |
| `Infrastructure.Core/DependencyInjection.cs` | Accepts `IConfiguration`; registers `OutboxProcessorOptions` and `OutboxProcessor` hosted service |
| `SpendBear.Api/Program.cs` | Passes configuration to `AddInfrastructureCore()`; calls `OutboxTableInitializer` at startup; removed duplicate dispatcher registration |
| `SpendBear.Api/appsettings.json` | Added `"Outbox"` configuration section |
| 6 module DbContexts | Removed `IDomainEventDispatcher` constructor parameter |
| `Spending.Infrastructure/Extensions/ServiceCollectionExtensions.cs` | Registers `TransactionReadService` |
| `Analytics.Application/DependencyInjection.cs` | Registers `RebuildAnalyticsHandler` |
| `Analytics.Api/Controllers/AnalyticsController.cs` | Added `POST /api/analytics/rebuild` endpoint |

---

## Key Components

### BaseDbContext — Atomic Event Capture

Events are written to the outbox **in the same database transaction** as business data:

1. Collects domain events from all tracked `AggregateRoot` entities
2. Clears events from aggregates (prevents duplicate dispatch)
3. Calls `base.SaveChangesAsync()` to persist business data
4. Inserts outbox messages via raw SQL on the **same connection/transaction** (using `Database.GetDbConnection()` and `Database.CurrentTransaction?.GetDbTransaction()`)
5. Event type stored as `"{FullName}, {AssemblyName}"` for `Type.GetType()` resolution
6. Source module derived from DbContext class name (e.g., `SpendingDbContext` -> `"Spending"`)

### OutboxProcessor — Reliable Dispatch

A `BackgroundService` that continuously polls for unprocessed messages:

- **Polling:** `SELECT ... WHERE processed_at IS NULL AND retry_count < @maxRetries ORDER BY created_at ASC LIMIT @batchSize FOR UPDATE SKIP LOCKED`
- **Row locking:** `FOR UPDATE SKIP LOCKED` ensures safe concurrent processing if multiple instances run
- **Deserialization:** `Type.GetType(eventType)` -> `JsonSerializer.Deserialize(payload, type)` -> cast to `IDomainEvent`
- **Dispatch:** Creates a service scope and calls `IDomainEventDispatcher.DispatchAsync()` (reuses existing event handlers — no handler changes needed)
- **On success:** Sets `processed_at = NOW()`
- **On failure:** Increments `retry_count`, records `last_error`
- **Dead letters:** Messages exceeding max retries remain in the table for manual investigation
- **Cleanup:** Periodically deletes processed messages older than the configured retention period

**Important:** The Npgsql DataReader is fully disposed before committing the transaction to avoid `NpgsqlOperationInProgressException`.

### ITransactionReadService — Cross-Module Read Contract

Defined in `SharedKernel` to avoid direct module-to-module dependencies:

```csharp
public record TransactionReadModel(
    Guid TransactionId, Guid UserId, decimal Amount,
    string Currency, int Type, Guid CategoryId, DateTime Date);

public interface ITransactionReadService
{
    IAsyncEnumerable<TransactionReadModel> GetAllTransactionsAsync(
        Guid userId, CancellationToken cancellationToken);
    Task<List<Guid>> GetAllUserIdsAsync(CancellationToken cancellationToken);
}
```

Implementation in `Spending.Infrastructure` uses `AsNoTracking()` and `IAsyncEnumerable` for memory-efficient streaming.

### RebuildAnalyticsHandler — Data Recovery

Recomputes analytics snapshots from source transaction data:

1. Determines target users (single user from JWT, or all users if `UserId` is null)
2. For each user:
   - Fetches and deletes all existing monthly snapshots
   - Streams all transactions via `ITransactionReadService`
   - Groups by month using a `MonthAccumulator` (totals + category breakdowns)
   - Creates new `AnalyticSnapshot` aggregates via the domain factory method
   - Saves all new snapshots in a single batch

---

## Configuration

In `appsettings.json`:

```json
"Outbox": {
    "PollingIntervalMs": 1000,
    "BatchSize": 50,
    "MaxRetryCount": 5
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `PollingIntervalMs` | 1000 | How often the processor checks for new messages (ms) |
| `BatchSize` | 50 | Maximum messages to process per polling cycle |
| `MaxRetryCount` | 5 | Attempts before a message becomes a dead letter |

For tests, `PollingIntervalMs` is overridden to `200` for faster event processing.

---

## API Endpoint

### Rebuild Analytics

```
POST /api/analytics/rebuild
Authorization: Bearer <token>
```

Rebuilds analytics snapshots for the authenticated user by reprocessing all their transactions.

**Response (200):**
```json
{ "message": "Analytics rebuilt successfully" }
```

**Response (400):** Returns error details if the rebuild fails.

**Response (401):** If no valid JWT is provided.

---

## Verification Checklist

1. **Build:** `dotnet build` — no compilation errors after DbContext constructor changes
2. **Startup:** Outbox table created in `shared` schema; `OutboxProcessor` logs startup message
3. **Event flow:** Create a transaction via `POST /api/spending/transactions`, then:
   - Row appears in `shared.outbox_messages` with `TransactionCreatedEvent` type
   - Within ~1s, `processed_at` is populated
   - `GET /api/analytics/summary/monthly` shows updated totals
4. **Rebuild:** `POST /api/analytics/rebuild` recomputes snapshots to match actual transactions
5. **Failure recovery:** If a handler fails, `retry_count` increments and the event is retried on next poll
6. **Existing tests:** `dotnet test` passes (unit tests mock `IUnitOfWork`, not `BaseDbContext`)

---

## Known Considerations

- **DomainEvent deserialization:** `EventId` and `OccurredOn` are `{ get; }` properties (not `init`), so they get regenerated on deserialization. No handler currently checks these values. Can be improved later by adding `init` setters.
- **Budgets handler bug (out of scope):** The Budgets `TransactionCreatedEventHandler` does not implement `IEventHandler<TransactionCreatedEvent>` — it's registered as a plain scoped service, so it's never called by the domain event dispatcher. This is a separate fix.
- **Single-instance assumption:** While `FOR UPDATE SKIP LOCKED` supports concurrent instances, the current deployment is single-instance. No additional coordination is needed.

---

## References

- [Transactional Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Technical Architecture](./architecture.md)
- [Analytics Module Summary](./ANALYTICS_MODULE_SUMMARY.md)
