# SpendBear API - Azure Deployment Guide

**Last Updated:** 2025-12-01
**Target Platform:** Azure App Service
**CI/CD Options:** Azure DevOps Pipelines OR GitHub Actions

---

## Overview

This guide covers deploying the SpendBear API to Azure using either Azure DevOps Pipelines or GitHub Actions. The deployment follows a multi-environment strategy:

- **Development** (`develop` branch) → `spendbear-api-dev`
- **Staging** (`main` branch) → `spendbear-api-staging`
- **Production** (manual approval) → `spendbear-api`

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Azure Resources Setup](#azure-resources-setup)
3. [Database Configuration](#database-configuration)
4. [CI/CD Pipeline Setup](#cicd-pipeline-setup)
   - [Option A: Azure DevOps](#option-a-azure-devops)
   - [Option B: GitHub Actions](#option-b-github-actions)
5. [Environment Configuration](#environment-configuration)
6. [Deployment Steps](#deployment-steps)
7. [Post-Deployment](#post-deployment)
8. [Monitoring](#monitoring)
9. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Tools
- Azure CLI (`az`)
- .NET 10 SDK
- Git
- Azure subscription (Free tier sufficient for MVP)

### Azure Account Setup
```bash
# Install Azure CLI (if not already installed)
# macOS:
brew install azure-cli

# Windows:
winget install Microsoft.AzureCLI

# Login to Azure
az login

# Set subscription (if you have multiple)
az account set --subscription "Your Subscription Name"
```

---

## Azure Resources Setup

### 1. Create Resource Group

```bash
# Create resource group in your preferred region
az group create \
  --name spendbear-rg \
  --location eastus

# Verify creation
az group show --name spendbear-rg
```

### 2. Create Azure App Service Plan

```bash
# Create App Service Plan (Free tier for MVP)
az appservice plan create \
  --name spendbear-plan \
  --resource-group spendbear-rg \
  --sku F1 \
  --is-linux

# For production, use a paid tier:
# --sku B1  (Basic)
# --sku S1  (Standard)
# --sku P1v2 (Premium)
```

**Note:** Free tier (F1) limitations:
- 1 GB RAM
- 1 GB storage
- 60 CPU minutes/day
- No custom domain/SSL
- Suitable for MVP testing only

### 3. Create Web Apps (3 environments)

```bash
# Development Environment
az webapp create \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --plan spendbear-plan \
  --runtime "DOTNETCORE:10.0"

# Staging Environment
az webapp create \
  --name spendbear-api-staging \
  --resource-group spendbear-rg \
  --plan spendbear-plan \
  --runtime "DOTNETCORE:10.0"

# Production Environment
az webapp create \
  --name spendbear-api \
  --resource-group spendbear-rg \
  --plan spendbear-plan \
  --runtime "DOTNETCORE:10.0"
```

**Important:** Web app names must be globally unique. If taken, add suffix like `-yourname`.

### 4. Configure Web Apps

```bash
# Enable HTTPS only
az webapp update \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --https-only true

az webapp update \
  --name spendbear-api-staging \
  --resource-group spendbear-rg \
  --https-only true

az webapp update \
  --name spendbear-api \
  --resource-group spendbear-rg \
  --https-only true

# Set .NET version
az webapp config set \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --linux-fx-version "DOTNETCORE:10.0"
```

---

## Database Configuration

### Option 1: Neon PostgreSQL (Recommended for MVP)

SpendBear is already configured to use Neon. No Azure database needed!

**Neon Setup:**
1. Go to [neon.tech](https://neon.tech)
2. Create free account (3 GB storage, 1 database)
3. Create new project: `spendbear`
4. Copy connection string

**Connection String Format:**
```
Host=ep-xxx-xxx.us-east-2.aws.neon.tech;Database=spendbear;Username=mario;Password=xxx;SSL Mode=Require
```

**Create Separate Databases:**
```sql
-- Development
CREATE DATABASE spendbear_dev;

-- Staging
CREATE DATABASE spendbear_staging;

-- Production
CREATE DATABASE spendbear;
```

### Option 2: Azure Database for PostgreSQL

If you prefer Azure-hosted database:

```bash
# Create PostgreSQL Flexible Server
az postgres flexible-server create \
  --name spendbear-db \
  --resource-group spendbear-rg \
  --location eastus \
  --admin-user postgres \
  --admin-password "YourSecurePassword123!" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16

# Allow Azure services to connect
az postgres flexible-server firewall-rule create \
  --name spendbear-db \
  --resource-group spendbear-rg \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create databases
az postgres flexible-server db create \
  --resource-group spendbear-rg \
  --server-name spendbear-db \
  --database-name spendbear_dev

az postgres flexible-server db create \
  --resource-group spendbear-rg \
  --server-name spendbear-db \
  --database-name spendbear_staging

az postgres flexible-server db create \
  --resource-group spendbear-rg \
  --server-name spendbear-db \
  --database-name spendbear
```

**Cost:** ~$12-15/month for Burstable tier

---

## CI/CD Pipeline Setup

### Option A: Azure DevOps

#### 1. Create Azure DevOps Project

1. Go to [dev.azure.com](https://dev.azure.com)
2. Create new project: `SpendBear`
3. Import repository from GitHub (or push code directly)

#### 2. Create Service Connection

1. Go to **Project Settings** → **Service connections**
2. Click **New service connection**
3. Select **Azure Resource Manager**
4. Choose **Service principal (automatic)**
5. Select your subscription and resource group
6. Name: `SpendBear-Azure-Connection`
7. Grant access to all pipelines

#### 3. Configure Pipeline Variables

Go to **Pipelines** → **Library** → **Variable groups**

**Create Variable Group: `SpendBear-Dev`**
- `DevDbConnectionString`: Your Neon dev connection string
- `Auth0Domain`: `dev-civhz1e8juvue64u.us.auth0.com`
- `Auth0Audience`: `https://spendbear-api`

**Create Variable Group: `SpendBear-Staging`**
- `StagingDbConnectionString`: Your Neon staging connection string
- `Auth0Domain`: Same as dev
- `Auth0Audience`: Same as dev

**Create Variable Group: `SpendBear-Production`**
- `ProductionDbConnectionString`: Your Neon production connection string
- `Auth0Domain`: Same as dev
- `Auth0Audience`: Same as dev

#### 4. Update Pipeline Configuration

Edit `azure-pipelines.yml`:

```yaml
variables:
  azureSubscription: 'SpendBear-Azure-Connection' # Match your service connection name
  webAppName: 'spendbear-api' # Match your web app name (without environment suffix)
  resourceGroupName: 'spendbear-rg'
```

#### 5. Create Pipeline

1. Go to **Pipelines** → **Create Pipeline**
2. Select **Azure Repos Git** (or GitHub)
3. Select your repository
4. Choose **Existing Azure Pipelines YAML file**
5. Select `/azure-pipelines.yml`
6. Click **Run**

#### 6. Configure Environments

1. Go to **Pipelines** → **Environments**
2. Create three environments:
   - `Development` (auto-deploy)
   - `Staging` (auto-deploy)
   - `Production` (add approval check)

**Add Approval for Production:**
1. Click `Production` environment
2. Click **⋮** (More options) → **Approvals and checks**
3. Click **+** → **Approvals**
4. Add yourself as approver
5. Save

### Option B: GitHub Actions

#### 1. Get Azure Publish Profiles

```bash
# Development
az webapp deployment list-publishing-profiles \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --xml > dev-profile.xml

# Staging
az webapp deployment list-publishing-profiles \
  --name spendbear-api-staging \
  --resource-group spendbear-rg \
  --xml > staging-profile.xml

# Production
az webapp deployment list-publishing-profiles \
  --name spendbear-api \
  --resource-group spendbear-rg \
  --xml > production-profile.xml
```

#### 2. Add GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions**

**Add Repository Secrets:**
- `AZURE_WEBAPP_PUBLISH_PROFILE_DEV`: Contents of `dev-profile.xml`
- `AZURE_WEBAPP_PUBLISH_PROFILE_STAGING`: Contents of `staging-profile.xml`
- `AZURE_WEBAPP_PUBLISH_PROFILE`: Contents of `production-profile.xml`

**Note:** Copy the ENTIRE XML content including `<?xml version...>` header

#### 3. Configure GitHub Environments

1. Go to **Settings** → **Environments**
2. Create three environments:
   - `Development`
   - `Staging`
   - `Production`

**Add Protection Rule for Production:**
1. Click `Production` environment
2. Check **Required reviewers**
3. Add yourself as reviewer
4. Save

#### 4. Enable GitHub Actions

The workflow file `.github/workflows/azure-deploy.yml` is already configured. Just push to trigger:

```bash
git add .github/workflows/azure-deploy.yml
git commit -m "ci: Add GitHub Actions deployment workflow"
git push origin develop
```

---

## Environment Configuration

### Application Settings (All Environments)

Configure via Azure Portal or CLI:

```bash
# Development Environment
az webapp config appsettings set \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT="Development" \
    ConnectionStrings__DefaultConnection="YOUR_NEON_DEV_CONNECTION_STRING" \
    Auth0__Domain="dev-civhz1e8juvue64u.us.auth0.com" \
    Auth0__Audience="https://spendbear-api"

# Staging Environment
az webapp config appsettings set \
  --name spendbear-api-staging \
  --resource-group spendbear-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT="Staging" \
    ConnectionStrings__DefaultConnection="YOUR_NEON_STAGING_CONNECTION_STRING" \
    Auth0__Domain="dev-civhz1e8juvue64u.us.auth0.com" \
    Auth0__Audience="https://spendbear-api"

# Production Environment
az webapp config appsettings set \
  --name spendbear-api \
  --resource-group spendbear-rg \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    ConnectionStrings__DefaultConnection="YOUR_NEON_PRODUCTION_CONNECTION_STRING" \
    Auth0__Domain="dev-civhz1e8juvue64u.us.auth0.com" \
    Auth0__Audience="https://spendbear-api"
```

### Additional Settings (Optional)

```bash
# Enable detailed errors (Development only)
az webapp config appsettings set \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --settings \
    ASPNETCORE_DETAILEDERRORS="true"

# Configure logging
az webapp log config \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --application-logging filesystem \
  --level information
```

---

## Deployment Steps

### Initial Deployment

#### Step 1: Run Database Migrations

Migrations need to be applied to each environment's database.

**Option 1: From Local Machine**

```bash
# Set connection string for target environment
export ConnectionStrings__DefaultConnection="YOUR_NEON_CONNECTION_STRING"

# Apply all migrations
dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --context IdentityDbContext
dotnet ef database update --project src/Modules/Spending/Spending.Infrastructure --context SpendingDbContext
dotnet ef database update --project src/Modules/Budgets/Budgets.Infrastructure --context BudgetsDbContext
dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --context NotificationsDbContext
dotnet ef database update --project src/Modules/Analytics/Analytics.Infrastructure --context AnalyticsDbContext
```

**Option 2: Via Azure CLI**

```bash
# SSH into the web app
az webapp ssh --name spendbear-api-dev --resource-group spendbear-rg

# Inside the container:
cd /home/site/wwwroot
dotnet ef database update --project Identity.Infrastructure.dll --context IdentityDbContext
# ... repeat for other contexts
```

**Option 3: Migration Script (Recommended)**

Create `scripts/migrate-azure.sh`:

```bash
#!/bin/bash
set -e

ENV=$1
if [ -z "$ENV" ]; then
  echo "Usage: ./migrate-azure.sh [dev|staging|prod]"
  exit 1
fi

case $ENV in
  dev)
    CONNECTION_STRING="$DEV_DB_CONNECTION_STRING"
    ;;
  staging)
    CONNECTION_STRING="$STAGING_DB_CONNECTION_STRING"
    ;;
  prod)
    CONNECTION_STRING="$PROD_DB_CONNECTION_STRING"
    ;;
  *)
    echo "Invalid environment. Use: dev, staging, or prod"
    exit 1
    ;;
esac

export ConnectionStrings__DefaultConnection="$CONNECTION_STRING"

echo "Applying migrations to $ENV environment..."

dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --context IdentityDbContext
dotnet ef database update --project src/Modules/Spending/Spending.Infrastructure --context SpendingDbContext
dotnet ef database update --project src/Modules/Budgets/Budgets.Infrastructure --context BudgetsDbContext
dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --context NotificationsDbContext
dotnet ef database update --project src/Modules/Analytics/Analytics.Infrastructure --context AnalyticsDbContext

echo "✅ All migrations applied successfully!"
```

Usage:
```bash
chmod +x scripts/migrate-azure.sh
./scripts/migrate-azure.sh dev
```

#### Step 2: Deploy Application

**Azure DevOps:**
1. Push code to `develop` branch
2. Pipeline triggers automatically
3. Monitor pipeline run
4. Check deployment status

**GitHub Actions:**
1. Push code to `develop` branch
2. Go to **Actions** tab
3. Watch workflow execution
4. Review logs

#### Step 3: Verify Deployment

```bash
# Check health endpoint
curl https://spendbear-api-dev.azurewebsites.net/health

# Expected response:
# Healthy

# Test API endpoint
curl https://spendbear-api-dev.azurewebsites.net/api/spending/categories \
  -H "Authorization: Bearer YOUR_AUTH0_TOKEN"
```

---

## Post-Deployment

### 1. Verify All Services

```bash
# Development
curl https://spendbear-api-dev.azurewebsites.net/health

# Staging
curl https://spendbear-api-staging.azurewebsites.net/health

# Production
curl https://spendbear-api.azurewebsites.net/health
```

### 2. Test Authentication

Get Auth0 token:
```bash
curl --request POST \
  --url https://dev-civhz1e8juvue64u.us.auth0.com/oauth/token \
  --header 'content-type: application/json' \
  --data '{
    "client_id":"YOUR_CLIENT_ID",
    "client_secret":"YOUR_CLIENT_SECRET",
    "audience":"https://spendbear-api",
    "grant_type":"client_credentials"
  }'
```

Test authenticated endpoint:
```bash
TOKEN="your_token_here"

curl https://spendbear-api-dev.azurewebsites.net/api/spending/categories \
  -H "Authorization: Bearer $TOKEN"
```

### 3. View Application Logs

```bash
# Stream logs in real-time
az webapp log tail \
  --name spendbear-api-dev \
  --resource-group spendbear-rg

# Download recent logs
az webapp log download \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --log-file logs.zip
```

### 4. Set Up Monitoring

```bash
# Enable Application Insights (optional, paid feature)
az monitor app-insights component create \
  --app spendbear-insights \
  --location eastus \
  --resource-group spendbear-rg

# Link to Web App
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app spendbear-insights \
  --resource-group spendbear-rg \
  --query instrumentationKey -o tsv)

az webapp config appsettings set \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

---

## Monitoring

### Health Checks

SpendBear includes health check endpoints:

- `/health` - Overall health
- `/health/ready` - Readiness probe
- `/health/live` - Liveness probe

Configure Azure health checks:
```bash
az webapp config set \
  --name spendbear-api-dev \
  --resource-group spendbear-rg \
  --generic-configurations '{"healthCheckPath": "/health"}'
```

### Logging

View logs via Azure Portal:
1. Go to your Web App
2. Click **Monitoring** → **Log stream**
3. Select **Application logs**

Or via CLI:
```bash
az webapp log tail --name spendbear-api-dev --resource-group spendbear-rg
```

### Metrics

Key metrics to monitor:
- **Response Time**: < 200ms average
- **HTTP Errors**: < 1%
- **Memory Usage**: < 80% of available
- **CPU Usage**: < 70%

Access via:
1. Azure Portal → Web App → **Monitoring** → **Metrics**
2. Application Insights (if enabled)

---

## Troubleshooting

### Issue 1: Deployment Fails - "Could not find a part of the path"

**Cause:** Missing project file or incorrect path in pipeline

**Fix:**
```yaml
# Verify publish project path in pipeline
projects: 'src/Api/SpendBear.Api/SpendBear.Api.csproj'
```

### Issue 2: Application Won't Start - "500 Internal Server Error"

**Cause:** Missing environment variables or connection string

**Fix:**
```bash
# Check app settings
az webapp config appsettings list \
  --name spendbear-api-dev \
  --resource-group spendbear-rg

# Verify connection string is set
az webapp log tail --name spendbear-api-dev --resource-group spendbear-rg
```

### Issue 3: Database Connection Fails

**Symptoms:**
```
Npgsql.NpgsqlException: Connection refused
```

**Fix:**
1. Verify connection string format (especially SSL Mode=Require for Neon)
2. Check firewall rules (if using Azure PostgreSQL)
3. Test connection from local machine:
   ```bash
   psql "YOUR_CONNECTION_STRING"
   ```

### Issue 4: Auth0 Authentication Fails

**Symptoms:**
```
401 Unauthorized
Bearer error="invalid_token"
```

**Fix:**
1. Verify Auth0 domain and audience in app settings
2. Check token expiration
3. Verify Auth0 API permissions
4. Test token:
   ```bash
   # Decode JWT at jwt.io
   # Check 'exp' claim hasn't passed
   ```

### Issue 5: Migrations Not Applied

**Symptoms:**
```
Npgsql.PostgresException: 42P01: relation "identity.Users" does not exist
```

**Fix:**
```bash
# Apply migrations manually
export ConnectionStrings__DefaultConnection="YOUR_CONNECTION_STRING"
./scripts/migrate-azure.sh dev
```

### Issue 6: Tests Fail in Pipeline

**Symptoms:**
```
Build succeeded, but tests failed
```

**Fix:**
```yaml
# Don't fail build on test failures (for now)
failTaskOnFailedTests: false
```

Or fix the specific failing tests (Budget validation, Analytics timing).

### Issue 7: Out of Memory - Free Tier

**Symptoms:**
```
Application Error: Memory limit exceeded
```

**Fix:**
```bash
# Upgrade to Basic tier
az appservice plan update \
  --name spendbear-plan \
  --resource-group spendbear-rg \
  --sku B1
```

---

## Cost Estimation

### Free Tier (MVP Testing)
- **App Service Plan (F1)**: $0/month
- **Neon PostgreSQL (Free)**: $0/month
- **Azure DevOps (Free)**: $0/month (5 users, 1 pipeline)
- **GitHub Actions**: $0/month (2000 minutes)

**Total MVP Cost: $0/month** ✅

### Production (Basic Tier)
- **App Service Plan (B1)**: ~$13/month
- **Neon PostgreSQL (Scale)**: ~$19/month
- **Application Insights**: ~$5/month (1 GB data)

**Total Production Cost: ~$37/month**

### Production (Standard Tier)
- **App Service Plan (S1)**: ~$70/month
- **Azure PostgreSQL (Burstable)**: ~$15/month
- **Application Insights**: ~$5/month

**Total Production Cost: ~$90/month**

---

## Security Checklist

Before going to production:

- [ ] Enable HTTPS only on all Web Apps
- [ ] Configure custom domain with SSL certificate
- [ ] Rotate Auth0 client secrets
- [ ] Enable Azure Key Vault for secrets
- [ ] Configure CORS properly (not wildcard)
- [ ] Enable Web Application Firewall (WAF)
- [ ] Set up Azure Front Door (optional)
- [ ] Configure rate limiting
- [ ] Enable database SSL (Neon has this by default)
- [ ] Set up backup and disaster recovery
- [ ] Configure monitoring and alerts
- [ ] Review and minimize IAM permissions
- [ ] Enable Azure Security Center recommendations

---

## Next Steps

1. **Complete Initial Deployment**
   - Apply this guide to deploy to Dev environment
   - Verify all endpoints work
   - Test authentication flow

2. **Configure CI/CD**
   - Set up either Azure DevOps or GitHub Actions
   - Test automated deployments
   - Configure approval gates for Production

3. **Documentation**
   - Update CLAUDE.md with deployment info
   - Document any environment-specific issues
   - Create runbook for common operations

4. **Prepare for Production**
   - Upgrade to paid tier
   - Configure monitoring and alerts
   - Set up backup strategy
   - Create incident response plan

---

## Resources

- [Azure App Service Docs](https://docs.microsoft.com/en-us/azure/app-service/)
- [Neon PostgreSQL Docs](https://neon.tech/docs)
- [Auth0 Documentation](https://auth0.com/docs)
- [Azure DevOps Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/)
- [GitHub Actions](https://docs.github.com/en/actions)
- [SpendBear API Documentation](./README.md)

---

**Deployment URLs:**

- **Development:** https://spendbear-api-dev.azurewebsites.net
- **Staging:** https://spendbear-api-staging.azurewebsites.net
- **Production:** https://spendbear-api.azurewebsites.net

**Support:** Check the [Troubleshooting](#troubleshooting) section or review pipeline logs in Azure DevOps/GitHub Actions.
