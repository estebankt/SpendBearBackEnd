# SpendBear — Backend API

A personal finance management API built to demonstrate production-grade backend engineering: domain isolation, reliable event delivery, and zero-downtime deployments on a monolithic codebase that can scale to services if needed.

---

## Architecture & Design Decisions

**Modular Monolith over Microservices**
The system is decomposed into six bounded contexts (Identity, Spending, Budgets, Analytics, Notifications, StatementImport), each with strict layer separation (Domain → Application → Infrastructure → Api). Modules communicate exclusively via domain events — no direct project references across module boundaries. This enforces the same decoupling discipline as microservices without the operational overhead of distributed systems at this stage.

**CQRS without MediatR**
Commands and queries are organized into vertical feature slices (`Application/Features/{FeatureName}/`). Handlers are registered directly with the DI container. MediatR was deliberately excluded to keep the dispatch path explicit and avoid pipeline magic that obscures behavior during debugging.

**Outbox Pattern for Reliable Event Delivery**
Domain events are written atomically to a `shared.outbox_messages` table within the same database transaction as the state change. A background `OutboxProcessor` polls and dispatches events asynchronously. This eliminates the dual-write problem without requiring a distributed transaction coordinator.

**Result\<T\> over Exceptions**
Expected failures (validation errors, not-found, business rule violations) are modelled as `Result<T>` return types. Exceptions are reserved for invariant violations and unexpected infrastructure failures, caught by a global exception handler that maps to RFC 9457 Problem Details responses.

**Auth0 for Identity**
Authentication is fully delegated to Auth0 (JWT Bearer). The backend resolves an internal `UserId` from the `sub` claim on first login and attaches it to the request context via `UserResolutionMiddleware`. No password storage or token issuance in-process.

---

## Core Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API |
| Database | PostgreSQL 16 (Neon in cloud) |
| ORM | Entity Framework Core 10 + Npgsql |
| Authentication | Auth0 (JWT Bearer) |
| Logging | Serilog (structured, JSON in production) |
| API Docs | Scalar / OpenAPI 3 |
| Containerization | Docker + Docker Compose |
| CI/CD | Azure DevOps Pipelines |
| Hosting | Azure Web App + Azure Container Registry |

---

## Engineering Standards

**Testing**
- Unit tests cover domain invariants, value objects, and application handlers for all six modules
- Integration tests use TestContainers (PostgreSQL) via `WebApplicationFactory` — no mocks for database interactions
- API-level tests exercise full vertical slices including outbox event processing

**CI/CD Pipeline** (`azure-pipelines.yml`)
- Four-stage pipeline: Build & Test → Migrate → Deploy Staging → Deploy Production
- EF Core migration bundles are compiled at build time and executed as a separate stage before deployment — migrations never run on app startup in non-development environments
- Smoke tests (`/health`) gate each deployment stage
- Production deploys require explicit manual trigger (`deployTarget: production`)

**API Documentation**
Scalar UI is available at `/scalar` in development. OpenAPI spec is auto-generated from endpoint metadata.

---

## Getting Started

**Prerequisites:** Docker, Docker Compose

```bash
# Start PostgreSQL and pgAdmin
docker compose up -d postgres pgadmin

# Run the API (applies migrations and seeds dev data automatically)
dotnet run --project src/Api/SpendBear.Api/SpendBear.Api.csproj
```

API: `http://localhost:5109`
Scalar docs: `http://localhost:5109/scalar`
pgAdmin: `http://localhost:5050`

In development, Auth0 validation is bypassed by `DevelopmentAuthMiddleware`. All requests run as the seeded test user.

---

## Documentation

| Document | Description |
|---|---|
| [Architecture](./documentation/architecture.md) | Detailed module boundaries, event flow, and data access patterns |
| [API Reference](./documentation/api.md) | Endpoint specs, request/response shapes, error codes |
| [Azure Deployment Guide](./documentation/AZURE_DEPLOYMENT_GUIDE.md) | Infrastructure setup, pipeline variables, environment configuration |
| [Project Status](./documentation/PROJECT_STATUS.md) | Implementation progress by module |
| [Outbox Pattern](./documentation/OUTBOX_PATTERN_SUMMARY.md) | Event delivery design and failure handling |
| Module Summaries | [Spending](./documentation/SPENDING_MODULE_SUMMARY.md) · [Budgets](./documentation/BUDGETS_MODULE_SUMMARY.md) · [Notifications](./documentation/NOTIFICATIONS_MODULE_SUMMARY.md) · [Analytics](./documentation/ANALYTICS_MODULE_SUMMARY.md) · [StatementImport](./documentation/STATEMENT_IMPORT_MODULE_SUMMARY.md) |
