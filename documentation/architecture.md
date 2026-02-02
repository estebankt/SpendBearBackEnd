# Technical Architecture - SpendBear

## System Overview

SpendBear is designed as a **Modular Monolith** that can evolve into microservices. The architecture emphasizes:
- **Module isolation** with clear boundaries
- **Event-driven communication** between modules
- **CQRS** for separating reads and writes
- **Domain-Driven Design** for complex business logic

## Architectural Principles

### 1. Modular Monolith
- Single deployable unit with logical module separation
- Modules communicate via events only (no direct references)
- Each module owns its data and exposes it via APIs
- Prepared for future extraction to microservices

### 2. Domain-Driven Design (DDD)
- **Aggregates** enforce business invariants
- **Value Objects** for immutable domain concepts
- **Domain Events** for cross-aggregate communication
- **Repositories** for aggregate persistence

### 3. CQRS (without MediatR)
- **Commands** modify state (write side)
- **Queries** retrieve data (read side)
- **Projections** for optimized read models
- Direct handler injection, no mediator pattern

### 4. Event-Driven Architecture
- **Domain Events** for intra-module communication
- **Integration Events** for cross-module boundaries
- **Outbox Pattern** for reliable event publishing
- **Event Sourcing** considered for audit trail (future)

## Module Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     API Gateway                          │
│                  (Authentication)                        │
└─────────────┬───────────┬───────────┬──────────┬────────┘
              │           │           │          │
     ┌────────▼──────┐ ┌──▼───────┐ ┌▼────────┐ ┌▼────────┐ ┌▼───────────────┐
     │   Identity    │ │ Spending │ │ Budgets │ │Analytics│ │StatementImport │
     │    Module     │ │  Module  │ │ Module  │ │ Module  │ │    Module       │
     └───────┬───────┘ └────┬─────┘ └────┬────┘ └────┬────┘ └───────┬────────┘
             │              │             │           │              │
     ┌───────▼──────────────▼─────────────▼───────────▼──────────────▼──┐
     │                  Event Bus (Kafka/In-Memory)                     │
     └──────────────────────────────────────────────────────────────────┘
             │              │             │           │              │
     ┌───────▼──────┐ ┌────▼─────┐ ┌────▼────┐ ┌────▼────┐ ┌──────▼─────┐
     │  PostgreSQL  │ │  Redis   │ │ Outbox  │ │SendGrid │ │  OpenAI    │
     │   (Neon)     │ │  Cache   │ │  Table  │ │  Email  │ │  (AI PDF)  │
     └──────────────┘ └──────────┘ └─────────┘ └─────────┘ └────────────┘
```

## Module Details

### Identity Module
**Responsibility**: User management and authentication

**Components**:
- User Aggregate (Auth0UserId, Preferences)
- Registration/Profile commands
- JWT validation middleware

**Events Published**:
- UserRegisteredEvent
- ProfileUpdatedEvent

### Spending Module (Core Domain)
**Responsibility**: Transaction and category management

**Components**:
- Transaction Aggregate
- Category Entity
- Money Value Object
- Transaction Repository

**Events Published**:
- TransactionCreatedEvent
- TransactionUpdatedEvent
- TransactionDeletedEvent
- CategoryCreatedEvent

**Events Consumed**: None (source of truth)

### Budgets Module
**Responsibility**: Budget limits and monitoring

**Components**:
- Budget Aggregate
- Budget Calculator Service
- Threshold Detector

**Events Published**:
- BudgetCreatedEvent
- BudgetThresholdReachedEvent
- BudgetExceededEvent

**Events Consumed**:
- TransactionCreatedEvent
- TransactionUpdatedEvent
- TransactionDeletedEvent

### Analytics Module
**Responsibility**: Reporting and insights

**Components**:
- Projection Handlers
- Snapshot Aggregator
- Trend Calculator

**Events Published**: None (read-only)

**Events Consumed**:
- TransactionCreatedEvent
- TransactionUpdatedEvent
- BudgetCreatedEvent

### Statement Import Module
**Responsibility**: AI-powered bank statement parsing and transaction import

**Components**:
- StatementUpload Aggregate (upload lifecycle management)
- ParsedTransaction Entity (AI-extracted transaction data)
- OpenAI Statement Parsing Service (GPT-4o-mini)
- PDF Text Extraction Service (PdfPig)
- Category Provider (cross-module integration with Spending)
- Transaction Creation Service (cross-module integration with Spending)
- Local File Storage Service

**Events Published**:
- StatementImportConfirmedEvent (contains confirmed transactions for creation in Spending module)

**Events Consumed**: None (initiates workflow, delegates to Spending module on confirmation)

**Cross-Module Dependencies**:
- Spending.Domain (ICategoryRepository for category lookup)
- Spending.Application (CreateTransactionHandler for transaction creation on confirm)

## Data Architecture

### Database Design
```sql
-- Identity Module
CREATE TABLE users (
    id UUID PRIMARY KEY,
    auth0_user_id VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    display_name VARCHAR(100),
    currency CHAR(3),
    locale VARCHAR(10),
    notification_preferences JSONB,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- Spending Module
CREATE TABLE transactions (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    amount BIGINT NOT NULL, -- stored in cents
    currency CHAR(3) NOT NULL,
    date DATE NOT NULL,
    category_id UUID NOT NULL,
    merchant VARCHAR(255),
    notes TEXT,
    receipt_url VARCHAR(500),
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    INDEX idx_user_date (user_id, date),
    INDEX idx_user_category (user_id, category_id)
);

CREATE TABLE categories (
    id UUID PRIMARY KEY,
    user_id UUID, -- NULL for system defaults
    name VARCHAR(50) NOT NULL,
    icon VARCHAR(50),
    color VARCHAR(7),
    is_default BOOLEAN,
    UNIQUE KEY uk_user_name (user_id, name)
);

-- Budgets Module
CREATE TABLE budgets (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    name VARCHAR(100),
    amount_limit BIGINT NOT NULL,
    period_type VARCHAR(20),
    start_date DATE,
    end_date DATE,
    category_id UUID,
    consumed_amount BIGINT DEFAULT 0,
    INDEX idx_user_period (user_id, start_date, end_date)
);

-- Statement Import Module
CREATE TABLE statement_import.StatementUploads (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    original_file_name VARCHAR(500) NOT NULL,
    stored_file_path VARCHAR(1000) NOT NULL,
    uploaded_at TIMESTAMP NOT NULL,
    status INTEGER NOT NULL, -- Uploading, Parsing, PendingReview, Confirmed, Failed, Cancelled
    error_message VARCHAR(2000),
    statement_month INTEGER,
    statement_year INTEGER,
    INDEX idx_user_id (user_id)
);

CREATE TABLE statement_import.ParsedTransactions (
    id UUID PRIMARY KEY,
    statement_upload_id UUID NOT NULL REFERENCES statement_import.StatementUploads(id) ON DELETE CASCADE,
    date TIMESTAMP NOT NULL,
    description VARCHAR(500) NOT NULL,
    amount BIGINT NOT NULL, -- stored as cents (value * 100)
    currency VARCHAR(3) NOT NULL,
    suggested_category_id UUID NOT NULL,
    confirmed_category_id UUID,
    original_text VARCHAR(2000),
    INDEX idx_statement_upload_id (statement_upload_id)
);

-- Outbox Pattern
CREATE TABLE outbox_messages (
    id UUID PRIMARY KEY,
    aggregate_id UUID NOT NULL,
    event_type VARCHAR(255) NOT NULL,
    payload JSONB NOT NULL,
    created_at TIMESTAMP NOT NULL,
    processed_at TIMESTAMP,
    retry_count INT DEFAULT 0,
    INDEX idx_unprocessed (processed_at) WHERE processed_at IS NULL
);
```

### Caching Strategy
- **Redis** for frequently accessed data
- Cache keys: `user:{id}:profile`, `user:{id}:dashboard`
- TTL: 5 minutes for dashboards, 30 minutes for profiles
- Cache-aside pattern with lazy loading

## Event Flow Examples

### Transaction Creation Flow
```
1. User submits transaction via API
2. CreateTransactionHandler validates input
3. Transaction aggregate created (domain validation)
4. Transaction saved to database
5. TransactionCreatedEvent saved to outbox
6. Transaction returns success
7. Background worker publishes event from outbox
8. Budgets module consumes event, updates consumed amount
9. If threshold reached, BudgetThresholdReachedEvent published
10. Notifications module sends alert to user
```

### Query Flow (Dashboard)
```
1. User requests dashboard
2. Check Redis cache for user:{id}:dashboard
3. If cache miss:
   a. Query analytics projections
   b. Aggregate current month data
   c. Store in cache with 5min TTL
4. Return dashboard data
```

## Cross-Cutting Concerns

### Authentication & Authorization
- Auth0 for identity provider
- JWT Bearer tokens
- Claims-based authorization
- User context from ClaimsPrincipal

### Error Handling
```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public Error Error { get; }
}

public class Error
{
    public string Code { get; }
    public string Message { get; }
    public ErrorType Type { get; }
}
```

### Logging
- Structured logging with Serilog
- Correlation IDs for request tracking
- Log levels: Debug (dev), Information (staging), Warning (prod)
- Sinks: Console, File, Application Insights

### Monitoring
- **Metrics**: Prometheus format
  - Request duration
  - Error rates
  - Event processing lag
- **Health Checks**: /health endpoint
- **Distributed Tracing**: OpenTelemetry (future)

## Security Architecture

### API Security
- HTTPS only (TLS 1.2+)
- Rate limiting per user
- CORS configuration for frontend
- API versioning headers

### Data Security
- Encryption at rest (PostgreSQL)
- PII data minimization
- Row-level security via user_id
- No sensitive data in logs

### Secret Management
- Azure Key Vault for production
- Environment variables for development
- Connection string encryption
- API key rotation policy

## Deployment Architecture

### Development
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:15
  redis:
    image: redis:7-alpine
  kafka:
    image: confluentinc/cp-kafka:latest
```

### Production (Azure)
- **API**: Azure Web App (Linux container)
- **Database**: PostgreSQL on Neon
- **Cache**: Azure Cache for Redis
- **Events**: Azure Event Hubs (Kafka-compatible)
- **Storage**: Azure Blob Storage (receipts)
- **CDN**: Azure CDN for static assets

### CI/CD Pipeline
```yaml
stages:
  - build:
      - Restore packages
      - Build solution
      - Run unit tests
  - test:
      - Run integration tests (TestContainers)
      - Code coverage check (>80%)
  - package:
      - Build Docker image
      - Push to Azure Container Registry
  - deploy:
      - Deploy to staging
      - Run smoke tests
      - Deploy to production (manual approval)
```

## Performance Considerations

### Database
- Connection pooling (100 connections)
- Query optimization with proper indexes
- Partition transactions table by year (future)
- Read replicas for analytics (future)

### Caching
- Redis for hot paths
- Response caching for static data
- ETag support for conditional requests

### Async Processing
- Background jobs for heavy operations
- Event processing with retries
- Batch imports in chunks of 1000

## Scalability Path

### Phase 1: Monolith (Current)
- Single deployment
- In-memory event bus
- Local caching

### Phase 2: Distributed Monolith
- Multiple instances
- Kafka for events
- Redis distributed cache
- Load balancer

### Phase 3: Microservices (Future)
- Extract Analytics as first service
- API Gateway (Ocelot/YARP)
- Service mesh (optional)
- Container orchestration (K8s)

## Disaster Recovery

### Backup Strategy
- Database: Daily automated backups (30-day retention)
- Point-in-time recovery: 7 days
- Geo-redundant storage

### RTO/RPO Targets
- RTO (Recovery Time): 4 hours
- RPO (Recovery Point): 1 hour
- Automated failover for critical services

## Technology Decisions

### Why Modular Monolith?
- Simpler deployment and debugging
- Lower operational overhead
- Easier local development
- Natural evolution to microservices

### Why No MediatR?
- Explicit over implicit
- Better IDE navigation
- Simpler debugging
- Less magic/reflection

### Why Outbox Pattern?
- Guaranteed event delivery
- Transactional consistency
- Resilience to failures
- Event ordering preservation

### Why PostgreSQL?
- ACID compliance
- JSON support for flexible schemas
- Excellent .NET support
- Cost-effective with Neon

## References
- [Domain-Driven Design](https://domainlanguage.com/ddd/)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [Outbox Pattern](https://microservices.io/patterns/data/transactional-outbox.html)
- [Modular Monolith](https://www.kamilgrzybek.com/design/modular-monolith-primer/)
