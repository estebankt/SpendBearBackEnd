# Tasks - SpendBear Development

## Current Sprint (Week 1-2: Foundation)

### High Priority
- [x] **Initialize .NET solution structure**
  - Details: Create solution with proper module separation
  - Estimate: 2h
  - Dependencies: None
  - Completed: 2025-11-29

- [x] **Setup shared kernel project**
  - Details: Base classes (AggregateRoot, DomainEvent, Result<T>)
  - Estimate: 3h
  - Dependencies: Solution structure
  - Completed: 2025-11-29

- [x] **Configure PostgreSQL with EF Core**
  - Details: Connection to Neon, base DbContext configuration
  - Estimate: 2h
  - Dependencies: Shared kernel

- [x] **Setup Auth0 integration**
  - Details: JWT validation, middleware, test endpoints
  - Estimate: 4h
  - Dependencies: API project setup
  - Subtasks:
    - [completed] Add authentication NuGet packages to API project
    - [completed] Configure JWT Bearer authentication
    - [completed] Update appsettings.json with Auth0 config
    - [completed] Add authentication middleware to API
    - [completed] Create test controller with [Authorize] attribute

### Medium Priority
- [x] **Configure Serilog logging**
  - Details: Structured logging, sinks, enrichers
  - Estimate: 2h
  - Dependencies: API setup
  - Subtasks:
    - [completed] Add Serilog NuGet packages to API
    - [completed] Configure Serilog in Program.cs
    - [completed] Update appsettings.json for Serilog
    - [completed] Verify logging output
  
- [x] **Setup Swagger documentation**
  - Details: OpenAPI spec, JWT auth in Swagger UI
  - Estimate: 1h
  - Dependencies: API setup
  - Subtasks:
    - [completed] Add Scalar.AspNetCore package to API
    - [completed] Configure OpenApi security scheme (JWT)
    - [completed] Enable Scalar UI in Program.cs

- [x] **Create Docker configuration**
  - Details: Dockerfile for API, docker-compose for local dev
  - Estimate: 2h
  - Dependencies: None
  - Subtasks:
    - [completed] Create Dockerfile for API
    - [completed] Create .dockerignore
    - [completed] Create docker-compose.yml (Postgres, API)

### Low Priority
- [ ] **Setup Azure DevOps pipeline**
  - Details: Build, test, push to ACR
  - Estimate: 3h
  - Dependencies: Docker setup
  
- [ ] **Configure Redis caching**
  - Details: Connection, basic cache service interface
  - Estimate: 2h
  - Dependencies: Infrastructure setup

## Next Sprint (Week 3-4: Identity Module)

### Ready
- [x] **Implement User aggregate**
  - Details: User entity with Auth0UserId, preferences
  - Estimate: 2h
  - Owner: Unassigned
  
- [x] **Create RegisterUser command**
  - Details: Command, handler, validator (vertical slice)
  - Estimate: 3h
  - Owner: Unassigned

- [x] **Implement UserRepository**

  - Details: EF Core implementation with unique constraints

  - Estimate: 2h

  - Owner: Unassigned
  
- [x] **Create GetProfile query**
  - Details: Query handler, DTO mapping
  - Estimate: 1h
  - Owner: Unassigned
  
- [x] **Setup Identity API endpoints**
  - Details: Controllers, route configuration
  - Estimate: 2h
  - Owner: Unassigned

## Backlog (Prioritized)

### Spending Module (Core) - COMPLETED ✅
- [x] Create Transaction aggregate (3h) - Completed: 2025-11-30
- [x] Implement Money value object (2h) - Completed: 2025-11-30
- [x] Create Category entity (2h) - Completed: 2025-11-30
- [x] Implement CreateTransaction feature (4h) - Completed: 2025-11-30
- [x] Implement CreateCategory feature (3h) - Completed: 2025-11-30
- [x] Create GetTransactions query with filtering (3h) - Completed: 2025-11-30
- [x] Setup transaction domain events (2h) - Completed: 2025-11-30
- [x] Create spending module migrations (1h) - Completed: 2025-11-30
- [x] Add transaction validation rules (2h) - Completed: 2025-11-30
- [x] Create Spending API controllers (2h) - Completed: 2025-11-30
- [x] Implement UpdateTransaction feature (3h) - Completed: 2025-11-30
- [x] Implement DeleteTransaction feature (2h) - Completed: 2025-11-30
- [x] Implement GetCategories query (2h) - Completed: 2025-11-30

### Budgets Module (Reactive)
- [ ] Create Budget aggregate (3h)
- [ ] Implement CreateBudget command (3h)
- [ ] Create TransactionCreatedEvent handler (4h)
- [ ] Implement budget threshold detection (3h)
- [ ] Create GetBudgetStatus query (2h)
- [ ] Setup budget notifications (3h)
- [ ] Implement budget period calculations (3h)
- [ ] Add budget module migrations (1h)

### Analytics Module (Projections)
- [ ] Design analytics snapshot schema (2h)
- [ ] Create event handlers for projections (4h)
- [ ] Implement monthly summary aggregation (3h)
- [ ] Create spending trends calculator (3h)
- [ ] Setup Redis caching for dashboards (2h)
- [ ] Implement category breakdowns (2h)
- [ ] Create analytics API endpoints (2h)

### Outbox Pattern Implementation
- [ ] Create outbox table schema (1h)
- [ ] Implement outbox repository (2h)
- [ ] Create background worker for publishing (4h)
- [ ] Add retry logic with exponential backoff (2h)
- [ ] Implement idempotency checks (2h)

### Notifications Module
- [ ] Setup SendGrid integration (2h)
- [ ] Create notification templates (3h)
- [ ] Implement event subscribers (3h)
- [ ] Add user preference checks (2h)
- [ ] Create notification audit log (1h)

### Frontend (Next.js)
- [ ] Initialize Next.js 15 project (1h)
- [ ] Setup TypeScript configuration (1h)
- [ ] Configure Auth0 for frontend (3h)
- [ ] Create transaction entry form (4h)
- [ ] Build dashboard with charts (6h)
- [ ] Implement budget status widgets (4h)
- [ ] Create responsive navigation (3h)
- [ ] Setup API client with interceptors (3h)

### Testing
- [ ] Setup xUnit test projects (1h)
- [ ] Create domain unit tests (4h)
- [ ] Setup TestContainers for integration tests (3h)
- [ ] Create repository integration tests (4h)
- [ ] Implement E2E test scenarios (6h)
- [ ] Add API contract tests (3h)

### Infrastructure & DevOps
- [ ] Setup Kafka locally with Docker (2h)
- [ ] Configure Prometheus monitoring (3h)
- [ ] Setup Grafana dashboards (3h)
- [ ] Create Azure infrastructure (Terraform) (4h)
- [ ] Configure Azure Key Vault for secrets (2h)
- [ ] Setup staging environment (3h)

## Completed

### 2025-11-29
- [x] Project requirements analysis (2h)
- [x] Architecture design documentation (3h)
- [x] Technology stack selection (1h)
- [x] Initialize .NET solution structure (2h)
  - Created SpendBear.sln with 19 projects
  - Four domain modules: Identity, Spending, Budgets, Analytics
  - Each module with Domain, Application, Infrastructure, Api layers
  - Main API project with controllers
- [x] Setup shared kernel project (3h)
  - Implemented Entity, AggregateRoot, ValueObject base classes
  - Created DomainEvent infrastructure with IDomainEvent
  - Added Result<T> pattern for error handling
  - Included Error, DomainException for structured errors
  - Created IRepository<T> interface for aggregates

## Technical Debt
- [ ] Refactor to use mapperly instead of manual mapping
- [ ] Add comments and verbosity to all code
- [ ] Optimize database indexes based on query patterns
- [ ] Implement circuit breaker for external services
- [ ] Add comprehensive API versioning strategy

## Ideas & Future Features
- [ ] CSV/OFX bank import functionality
- [ ] Receipt OCR with Azure Cognitive Services
- [ ] Spending predictions with ML
- [ ] Social features (compare with friends)
- [ ] Recurring transaction detection
- [ ] Bill reminder notifications
- [ ] Savings goals module
- [ ] Expense splitting for shared costs

## Notes
- Focus on vertical slices - complete features end-to-end
- Prioritize Identity and Spending modules for MVP
- Keep event schema stable - versioning is complex
- Test Outbox pattern thoroughly before production
- Consider using MassTransit if Kafka becomes too complex

## Task States
- **Ready**: Task is defined and can be started
- **In Progress**: Currently being worked on
- **Blocked**: Waiting on dependency or decision
- **Review**: Code complete, needs review
- **Done**: Completed and merged

## Estimation Guide
- 1h: Simple configuration or small feature
- 2h: Standard CRUD operation or integration
- 3h: Complex feature with validation
- 4h: Vertical slice with tests
- 6h: Multi-component feature

Last Updated: 2025-11-29
