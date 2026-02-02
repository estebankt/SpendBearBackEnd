# Deployment Guide - SpendBear

## Environments

### Development
- **Purpose**: Local development and testing
- **URL**: https://localhost:7001
- **Database**: Local PostgreSQL or Docker container
- **Cache**: Local Redis or in-memory
- **Events**: In-memory event bus

### Staging
- **Purpose**: Pre-production testing
- **URL**: https://spendbear-staging.azurewebsites.net
- **Database**: PostgreSQL on Neon (staging instance)
- **Cache**: Azure Cache for Redis (Basic tier)
- **Events**: Azure Service Bus

### Production
- **Purpose**: Live environment
- **URL**: https://api.spendbear.com
- **Database**: PostgreSQL on Neon (production instance)
- **Cache**: Azure Cache for Redis (Standard tier)
- **Events**: Azure Event Hubs (Kafka-compatible)

## Local Development Setup

### Prerequisites

- .NET 10 SDK
- Docker (for PostgreSQL, Redis, PgAdmin)
- EF Core CLI tools (`dotnet tool install --global dotnet-ef`)

### Environment Setup
```bash
# Clone repository
git clone https://github.com/yourusername/spendbear.git
cd spendbear

# Start infrastructure (PostgreSQL 16, Redis 7, PgAdmin 4)
docker-compose up -d

# Install dependencies
dotnet restore
```

No `.env` file or additional configuration is needed. The `appsettings.json` and `appsettings.Development.json` files ship with development defaults that match the docker-compose services.

### Running Locally
```bash
# Apply database migrations (all 6 modules)
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/Api/SpendBear.Api
dotnet ef database update --project src/Modules/Spending/Spending.Infrastructure --startup-project src/Api/SpendBear.Api
dotnet ef database update --project src/Modules/Budgets/Budgets.Infrastructure --startup-project src/Api/SpendBear.Api
dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/Api/SpendBear.Api
dotnet ef database update --project src/Modules/Analytics/Analytics.Infrastructure --startup-project src/Api/SpendBear.Api
dotnet ef database update --project src/Modules/StatementImport/StatementImport.Infrastructure --startup-project src/Api/SpendBear.Api

# Run API
dotnet run --project src/Api/SpendBear.Api
```

### Verifying the setup

- API docs: http://localhost:5109/scalar/v1
- PgAdmin: http://localhost:5050 (email: `admin@spendbear.com`, password: `admin`)
- Quick smoke test: `./scripts/quick-test.sh`

In development mode, `DevelopmentAuthMiddleware` bypasses Auth0 authentication and injects a test user automatically.

## Azure Infrastructure

### Resource Group Structure
```
rg-spendbear-prod/
├── app-spendbear-api          # Web App for API
├── app-spendbear-worker        # Web App for background workers
├── redis-spendbear             # Azure Cache for Redis
├── kv-spendbear                # Key Vault for secrets
├── acr-spendbear               # Container Registry
├── appi-spendbear              # Application Insights
├── eventhub-spendbear          # Event Hubs namespace
└── storage-spendbear           # Storage account for receipts
```

### Terraform Configuration
```hcl
# main.tf
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0"
    }
  }
  backend "azurerm" {
    resource_group_name  = "rg-spendbear-terraform"
    storage_account_name = "stspendbearterraform"
    container_name       = "tfstate"
    key                  = "prod.terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}

# Resource Group
resource "azurerm_resource_group" "main" {
  name     = "rg-spendbear-${var.environment}"
  location = var.location
}

# App Service Plan
resource "azurerm_service_plan" "main" {
  name                = "asp-spendbear-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  os_type             = "Linux"
  sku_name            = var.environment == "prod" ? "P1v3" : "B1"
}

# Web App for API
resource "azurerm_linux_web_app" "api" {
  name                = "app-spendbear-api-${var.environment}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.main.id

  site_config {
    always_on = var.environment == "prod"
    
    application_stack {
      docker_image     = "acrspendbear.azurecr.io/spendbear-api"
      docker_image_tag = var.api_image_tag
    }
  }

  app_settings = {
    "WEBSITES_ENABLE_APP_SERVICE_STORAGE" = "false"
    "DOCKER_REGISTRY_SERVER_URL"          = "https://acrspendbear.azurecr.io"
    "ConnectionStrings__Database"         = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.db_connection.id})"
    "ConnectionStrings__Redis"            = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.redis_connection.id})"
  }

  identity {
    type = "SystemAssigned"
  }
}
```

### Azure DevOps Pipeline
```yaml
# azure-pipelines.yml
trigger:
  branches:
    include:
      - main
      - develop
  paths:
    exclude:
      - docs/*
      - README.md

variables:
  - group: spendbear-variables
  - name: buildConfiguration
    value: 'Release'
  - name: dockerRegistryServiceConnection
    value: 'acr-spendbear'
  - name: imageRepository
    value: 'spendbear-api'
  - name: containerRegistry
    value: 'acrspendbear.azurecr.io'
  - name: tag
    value: '$(Build.BuildId)'

stages:
- stage: Build
  displayName: 'Build and Test'
  jobs:
  - job: BuildApi
    displayName: 'Build API'
    pool:
      vmImage: 'ubuntu-latest'
    steps:
    - task: UseDotNet@2
      inputs:
        packageType: 'sdk'
        version: '8.x'

    - task: DotNetCoreCLI@2
      displayName: 'Restore packages'
      inputs:
        command: 'restore'
        projects: '**/*.csproj'

    - task: DotNetCoreCLI@2
      displayName: 'Build solution'
      inputs:
        command: 'build'
        projects: '**/*.sln'
        arguments: '--configuration $(buildConfiguration) --no-restore'

    - task: DotNetCoreCLI@2
      displayName: 'Run unit tests'
      inputs:
        command: 'test'
        projects: '**/*Tests.csproj'
        arguments: '--configuration $(buildConfiguration) --no-build --collect:"XPlat Code Coverage"'

    - task: PublishCodeCoverageResults@1
      inputs:
        codeCoverageTool: 'Cobertura'
        summaryFileLocation: '$(Agent.TempDirectory)/**/coverage.cobertura.xml'

    - task: Docker@2
      displayName: 'Build Docker image'
      inputs:
        containerRegistry: '$(dockerRegistryServiceConnection)'
        repository: '$(imageRepository)'
        command: 'build'
        Dockerfile: 'src/Api/SpendBear.Api/Dockerfile'
        buildContext: '.'
        tags: |
          $(tag)
          latest

    - task: Docker@2
      displayName: 'Push Docker image'
      inputs:
        containerRegistry: '$(dockerRegistryServiceConnection)'
        repository: '$(imageRepository)'
        command: 'push'
        tags: |
          $(tag)
          latest

- stage: DeployStaging
  displayName: 'Deploy to Staging'
  dependsOn: Build
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/develop'))
  jobs:
  - deployment: DeployToStaging
    displayName: 'Deploy to Staging'
    environment: 'staging'
    pool:
      vmImage: 'ubuntu-latest'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebAppContainer@1
            inputs:
              azureSubscription: 'Azure-Subscription'
              appName: 'app-spendbear-api-staging'
              containers: '$(containerRegistry)/$(imageRepository):$(tag)'

          - task: AzureAppServiceManage@0
            displayName: 'Run database migrations'
            inputs:
              azureSubscription: 'Azure-Subscription'
              action: 'Start Azure App Service'
              webAppName: 'app-spendbear-migration-staging'

- stage: DeployProduction
  displayName: 'Deploy to Production'
  dependsOn: DeployStaging
  condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
  jobs:
  - deployment: DeployToProduction
    displayName: 'Deploy to Production'
    environment: 'production'
    pool:
      vmImage: 'ubuntu-latest'
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureWebAppContainer@1
            inputs:
              azureSubscription: 'Azure-Subscription'
              appName: 'app-spendbear-api-prod'
              containers: '$(containerRegistry)/$(imageRepository):$(tag)'
              slotName: 'staging'

          - task: AzureAppServiceManage@0
            displayName: 'Swap slots'
            inputs:
              azureSubscription: 'Azure-Subscription'
              action: 'Swap Slots'
              webAppName: 'app-spendbear-api-prod'
              sourceSlot: 'staging'
              targetSlot: 'production'
```

## Configuration Management

### Application Settings
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "Database": "Retrieved from Key Vault",
    "Redis": "Retrieved from Key Vault",
    "EventHub": "Retrieved from Key Vault"
  },
  "Auth0": {
    "Domain": "spendbear.auth0.com",
    "Audience": "https://api.spendbear.com",
    "ClientId": "Retrieved from Key Vault",
    "ClientSecret": "Retrieved from Key Vault"
  },
  "SendGrid": {
    "ApiKey": "Retrieved from Key Vault",
    "FromEmail": "notifications@spendbear.com"
  },
  "ApplicationInsights": {
    "InstrumentationKey": "Retrieved from Key Vault"
  },
  "Outbox": {
    "ProcessingInterval": "00:00:10",
    "BatchSize": 100,
    "MaxRetries": 3
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "Window": "00:01:00"
  }
}
```

### Environment Variables
```bash
# .env.production
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
AZURE_CLIENT_ID=managed-identity-client-id
WEBSITE_ENABLE_SYNC_UPDATE_SITE=true
WEBSITE_RUN_FROM_PACKAGE=1
```

### Key Vault Secrets
```
spendbear-db-connection
spendbear-redis-connection
spendbear-auth0-clientsecret
spendbear-sendgrid-apikey
spendbear-appinsights-key
```

## Database Migrations

### Creating Migrations
```bash
# Identity module
dotnet ef migrations add InitialIdentity \
  --project src/Modules/Identity/Identity.Infrastructure \
  --startup-project src/Api/SpendBear.Api

# Spending module
dotnet ef migrations add InitialSpending \
  --project src/Modules/Spending/Spending.Infrastructure \
  --startup-project src/Api/SpendBear.Api
```

### Applying Migrations
```bash
# Development
dotnet ef database update

# Production (via migration container)
docker run --rm \
  -e ConnectionStrings__Database=$PROD_CONNECTION_STRING \
  acrspendbear.azurecr.io/spendbear-migration:latest
```

### Migration Script
```csharp
// Program.cs in SpendBear.Migration project
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("Database")));
        
        services.AddDbContext<SpendingDbContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("Database")));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await identityDb.Database.MigrateAsync();
    
    var spendingDb = scope.ServiceProvider.GetRequiredService<SpendingDbContext>();
    await spendingDb.Database.MigrateAsync();
}
```

## Monitoring & Alerts

### Application Insights Queries
```kusto
// API Response Times
requests
| where timestamp > ago(1h)
| summarize percentiles(duration, 50, 90, 99) by bin(timestamp, 5m)

// Failed Requests
requests
| where timestamp > ago(1h) and success == false
| summarize count() by resultCode, bin(timestamp, 5m)

// Exception Tracking
exceptions
| where timestamp > ago(1h)
| summarize count() by type, outerMessage
| order by count_ desc
```

### Alert Rules
```json
{
  "alerts": [
    {
      "name": "High API Latency",
      "metric": "Response Time",
      "threshold": "2000ms",
      "window": "5 minutes",
      "severity": "Warning"
    },
    {
      "name": "Error Rate Spike",
      "metric": "Failed Requests",
      "threshold": "5%",
      "window": "5 minutes",
      "severity": "Critical"
    },
    {
      "name": "Database Connection Failures",
      "metric": "Dependency Failures",
      "threshold": "10",
      "window": "1 minute",
      "severity": "Critical"
    }
  ]
}
```

## Health Checks

### Implementation
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: configuration.GetConnectionString("Database"),
        name: "postgres",
        tags: new[] { "db", "critical" })
    .AddRedis(
        configuration.GetConnectionString("Redis"),
        name: "redis",
        tags: new[] { "cache" })
    .AddUrlGroup(
        new Uri("https://spendbear.auth0.com/.well-known/jwks.json"),
        name: "auth0",
        tags: new[] { "auth" });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("critical")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

### Monitoring Endpoints
```bash
# Overall health
GET /health

# Readiness probe (K8s)
GET /health/ready

# Liveness probe (K8s)
GET /health/live
```

## Rollback Procedures

### Database Rollback
```bash
# List migrations
dotnet ef migrations list

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Generate rollback script
dotnet ef migrations script CurrentMigration PreviousMigration --idempotent
```

### Application Rollback
```bash
# Azure Web App - Swap slots back
az webapp deployment slot swap \
  --name app-spendbear-api-prod \
  --resource-group rg-spendbear-prod \
  --slot staging \
  --target-slot production

# Docker - Deploy previous tag
docker pull acrspendbear.azurecr.io/spendbear-api:previous-tag
docker tag acrspendbear.azurecr.io/spendbear-api:previous-tag \
  acrspendbear.azurecr.io/spendbear-api:latest
docker push acrspendbear.azurecr.io/spendbear-api:latest
```

## Disaster Recovery

### Backup Strategy
```bash
# Database backup (Neon handles automatically)
# Manual backup if needed
pg_dump $DATABASE_URL > backup_$(date +%Y%m%d_%H%M%S).sql

# Redis backup
redis-cli --rdb /backup/dump.rdb
```

### Recovery Steps
1. **Assess damage scope**
2. **Activate incident response team**
3. **Switch to DR environment** (if available)
4. **Restore database from backup**
5. **Clear and rebuild caches**
6. **Verify data integrity**
7. **Gradual traffic migration**
8. **Post-mortem analysis**

## Security Checklist

### Pre-deployment
- [ ] Secrets in Key Vault
- [ ] SSL certificates valid
- [ ] Security headers configured
- [ ] CORS settings reviewed
- [ ] Rate limiting enabled
- [ ] Auth0 settings verified

### Post-deployment
- [ ] Penetration testing
- [ ] OWASP Top 10 scan
- [ ] Dependency vulnerabilities scan
- [ ] Log analysis for anomalies
- [ ] Performance benchmarks met
- [ ] Monitoring alerts active

## Performance Tuning

### Database
```sql
-- Add indexes for common queries
CREATE INDEX idx_transactions_user_date ON transactions(user_id, date DESC);
CREATE INDEX idx_transactions_user_category ON transactions(user_id, category_id);
CREATE INDEX idx_budgets_user_active ON budgets(user_id) WHERE end_date >= CURRENT_DATE;

-- Analyze query performance
EXPLAIN ANALYZE SELECT * FROM transactions WHERE user_id = ? AND date >= ?;
```

### Application
```csharp
// Connection pooling
services.AddDbContext<SpendBearDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(3);
    });
}, ServiceLifetime.Scoped);

// Redis configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "spendbear";
});
```

## Troubleshooting

### Common Issues
1. **Database connection timeouts**
   - Check connection pool settings
   - Verify network connectivity
   - Review query performance

2. **High memory usage**
   - Check for memory leaks
   - Review cache eviction policies
   - Analyze heap dumps

3. **Event processing delays**
   - Check outbox table backlog
   - Verify Event Hub throughput
   - Review retry policies

### Debug Commands
```bash
# View logs
az webapp log tail --name app-spendbear-api-prod --resource-group rg-spendbear-prod

# SSH into container
az webapp ssh --name app-spendbear-api-prod --resource-group rg-spendbear-prod

# Export metrics
az monitor metrics list --resource app-spendbear-api-prod --metric "Http2xx,Http4xx,Http5xx"
```

## Maintenance Windows

### Schedule
- **Regular maintenance**: Sunday 2-4 AM UTC
- **Critical patches**: As needed with 24h notice
- **Major upgrades**: Quarterly with 1 week notice

### Communication
- Status page: status.spendbear.com
- Email notifications to registered users
- In-app banners 24h before maintenance
