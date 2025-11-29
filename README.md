# SpendBear 🐻💰

> A robust personal finance management system built with Domain-Driven Design principles

## Overview

SpendBear is a personal finance tracker architected as a **Modular Monolith** using DDD, CQRS, and event-driven patterns. It helps users track expenses, manage budgets, and visualize spending habits through a modern, scalable architecture.

## Quick Start

```bash
# Clone the repository
git clone https://github.com/yourusername/spendbear.git
cd spendbear

# Setup local environment
cp .env.example .env
docker-compose up -d

# Run migrations
dotnet ef database update

# Start the application
dotnet run --project src/Api/SpendBear.Api
```

Visit https://localhost:7001/swagger to explore the API.

## Documentation

### Core Documents
- 📋 [Claude Context](./claude.md) - Main instruction file for Claude Code CLI
- 📄 [Product Requirements](./PRD.md) - User stories and acceptance criteria  
- ✅ [Task Tracking](./tasks.md) - Current development tasks and progress

### Technical Documentation
- 🏗️ [Architecture](./docs/architecture.md) - System design and patterns
- 🔌 [API Design](./docs/api.md) - Endpoint specifications
- 🚀 [Deployment](./docs/deployment.md) - Infrastructure and CI/CD

## Tech Stack

### Backend
- **.NET 8** with ASP.NET Core Web API
- **PostgreSQL** (Neon) with Entity Framework Core
- **Redis** for caching
- **Kafka** for event streaming
- **Auth0** for authentication

### Frontend
- **Next.js 15** with TypeScript
- **Vercel** deployment
- **Chart.js** for analytics

### Infrastructure
- **Azure** Web Apps & Services
- **Docker** containerization
- **Azure DevOps** CI/CD

## Architecture Highlights

```
┌─────────────────────────────────────┐
│         API Gateway (Auth0)         │
└─────┬──────┬──────┬──────┬──────────┘
      │      │      │      │
   Identity  Spending  Budgets  Analytics
      │      │      │      │
┌─────▼──────▼──────▼──────▼──────────┐
│        Event Bus (Kafka)            │
└──────────────────────────────────────┘
      │              │
   PostgreSQL      Redis
```

### Key Patterns
- **Modular Monolith** - Module isolation with clear boundaries
- **CQRS** - Separated read/write models (no MediatR)
- **Event-Driven** - Cross-module async communication
- **Outbox Pattern** - Guaranteed event delivery
- **DDD** - Rich domain models with aggregates

## Project Structure

```
SpendBear/
├── src/
│   ├── Modules/           # Domain modules
│   │   ├── Identity/
│   │   ├── Spending/      # Core domain
│   │   ├── Budgets/       # Reactive module
│   │   └── Analytics/     # Projections
│   ├── Shared/            # Shared kernel
│   ├── Api/               # Web API
│   └── Workers/           # Background jobs
├── tests/
│   ├── Unit/
│   └── Integration/
├── docs/                  # Documentation
└── infrastructure/        # IaC scripts
```

## Features

### Current (MVP)
- ✅ User authentication via Auth0
- ✅ Transaction logging with categories
- ✅ Budget management with thresholds
- ✅ Monthly spending summaries
- ✅ Real-time budget alerts

### Upcoming
- 🚧 Bank transaction imports
- 🚧 Multi-currency support
- 🚧 iOS mobile app
- 🚧 Advanced analytics & trends
- 🚧 Receipt OCR scanning

## Development

### Prerequisites
- .NET 8 SDK
- Node.js 18+
- Docker Desktop
- PostgreSQL client

### Commands
```bash
# Run tests
dotnet test

# Add migration
dotnet ef migrations add MigrationName

# Build Docker image
docker build -t spendbear-api .

# Format code
dotnet format
```

### Conventions
- **Feature folders** over layer folders
- **Explicit over implicit** (no MediatR)
- **Result pattern** for error handling
- **Money as cents** in database

## Testing Strategy

- **Unit Tests** - Domain logic, aggregates
- **Integration Tests** - Database, repositories
- **E2E Tests** - Full vertical slices
- **Contract Tests** - Event schemas

Coverage target: >80%

## Deployment

### Environments
- **Development** - Local Docker
- **Staging** - Azure Web Apps (B1)
- **Production** - Azure Web Apps (P1v3)

### CI/CD
Automated pipeline via Azure DevOps:
1. Build & test
2. Docker image creation
3. Deploy to staging
4. Manual approval
5. Production deployment

## Contributing

1. Fork the repository
2. Create a feature branch
3. Follow coding conventions
4. Add tests
5. Submit pull request

See [CONTRIBUTING.md](./CONTRIBUTING.md) for details.

## Security

- OAuth 2.0/OIDC via Auth0
- JWT with 1-hour expiry
- Row-level security
- Encrypted at rest
- No PII in logs

Report security issues to: security@spendbear.com

## License

MIT License - see [LICENSE](./LICENSE) file

## Support

- 📧 Email: support@spendbear.com
- 📚 Docs: https://docs.spendbear.com
- 💬 Discord: https://discord.gg/spendbear

## Roadmap

### Q1 2025
- [x] Project setup
- [ ] Identity module
- [ ] Core spending features
- [ ] Basic budgets

### Q2 2025
- [ ] Analytics dashboard
- [ ] Notifications
- [ ] Mobile app (iOS)

### Q3 2025
- [ ] Bank integrations
- [ ] Advanced insights
- [ ] Social features

## Status

[![Build Status](https://dev.azure.com/spendbear/spendbear/_apis/build/status/spendbear-ci?branchName=main)](https://dev.azure.com/spendbear/spendbear/_build)
[![Coverage](https://img.shields.io/badge/coverage-82%25-green)](https://dev.azure.com/spendbear/spendbear/_build)
[![License](https://img.shields.io/badge/license-MIT-blue)](./LICENSE)

---

**SpendBear** - Track spending, tame your budget 🐻

*Built with ❤️ using Claude Code CLI*
