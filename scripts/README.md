# SpendBear API Test Scripts

Automated testing scripts for the SpendBear API.

## Prerequisites

1. **PostgreSQL running** (via Docker):
   ```bash
   docker-compose up -d
   ```

2. **Migrations applied**:
   ```bash
   dotnet ef database update --project src/Modules/Identity/Identity.Infrastructure --startup-project src/Api/SpendBear.Api --context IdentityDbContext
   dotnet ef database update --project src/Modules/Spending/Spending.Infrastructure --startup-project src/Api/SpendBear.Api --context SpendingDbContext
   dotnet ef database update --project src/Modules/Budgets/Budgets.Infrastructure --startup-project src/Api/SpendBear.Api --context BudgetsDbContext
   dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/Api/SpendBear.Api --context NotificationsDbContext
   dotnet ef database update --project src/Modules/Analytics/Analytics.Infrastructure --startup-project src/Api/SpendBear.Api --context AnalyticsDbContext
   ```

3. **API running**:
   ```bash
   dotnet run --project src/Api/SpendBear.Api
   ```

## Scripts

### Quick Test (`quick-test.sh`)

Fast health check - tests basic functionality in ~2 seconds.

```bash
./scripts/quick-test.sh
```

**What it tests:**
- API is responding
- Create category
- Get categories
- Create transaction
- Get transactions
- Get analytics summary

**Use when:**
- Verifying API is working after changes
- Quick smoke test before deployment
- Continuous development workflow

### Full Test Suite (`test-api.sh`)

Comprehensive test of all 19 endpoints across 6 modules.

```bash
./scripts/test-api.sh
```

**What it tests:**

**Spending Module (6 endpoints):**
- ✓ POST `/api/spending/categories` - Create category
- ✓ GET `/api/spending/categories` - List categories
- ✓ POST `/api/spending/transactions` - Create transaction
- ✓ GET `/api/spending/transactions` - List transactions
- ✓ PUT `/api/spending/transactions/{id}` - Update transaction
- ○ DELETE `/api/spending/transactions/{id}` - Skipped to preserve test data

**Budgets Module (4 endpoints):**
- ✓ POST `/api/budgets` - Create budget
- ✓ GET `/api/budgets` - List budgets
- ✓ PUT `/api/budgets/{id}` - Update budget
- ○ DELETE `/api/budgets/{id}` - Skipped to preserve test data

**Notifications Module (2 endpoints):**
- ✓ GET `/api/notifications` - List notifications
- ✓ PUT `/api/notifications/{id}/read` - Mark as read

**Analytics Module (1 endpoint):**
- ✓ GET `/api/analytics/summary/monthly` - Get monthly summary

**Identity Module (2 endpoints):**
- ○ Info only - requires Auth0 user flow

**Event Flow Validation:**
- ✓ Transaction → Analytics integration
- ✓ Transaction → Budget integration
- ○ Budget → Notification integration (threshold-based)

**Use when:**
- Pre-deployment validation
- After major changes
- Regression testing
- Documenting API functionality

## Authentication

Both scripts use **development mode** (no authentication required).

The `DevelopmentAuthMiddleware` automatically injects a test user ID when no Authorization header is present.

**Test User ID:** `00000000-0000-0000-0000-000000000001`

All test data is scoped to this user.

## Output

The full test suite provides:
- ✅ Color-coded output (green = pass, red = fail)
- 📊 Test counter (passed/failed)
- 📈 Pass rate percentage
- 📝 Detailed response data for debugging

## Cleaning Test Data

To clean up test data between runs:

```sql
-- Connect to PostgreSQL
psql -h localhost -U testuser -d testdb

-- Delete test user's data
DELETE FROM spending."Transactions" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM budgets."Budgets" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM notifications."Notifications" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM analytics."AnalyticSnapshots" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM public.categories WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
```

Or use the cleanup script (if created):
```bash
./scripts/cleanup-test-data.sh
```

## Troubleshooting

### "API is not responding"
- Ensure API is running: `dotnet run --project src/Api/SpendBear.Api`
- Check port 5109 is not in use: `lsof -i :5109`

### "Failed to create category/transaction"
- Check PostgreSQL is running: `docker ps | grep postgres`
- Verify migrations: `dotnet ef migrations list --project src/Modules/Spending/Spending.Infrastructure --startup-project src/Api/SpendBear.Api --context SpendingDbContext`
- Check API logs for errors

### "Analytics snapshot not found"
- This is normal if no transactions exist yet
- Wait 2-3 seconds for event processing
- Events are processed asynchronously

### "Permission denied"
- Make scripts executable: `chmod +x scripts/*.sh`

## Examples

### Run quick test before committing changes:
```bash
./scripts/quick-test.sh && git add . && git commit -m "fix: ..."
```

### Run full suite and save results:
```bash
./scripts/test-api.sh | tee test-results.log
```

### Test specific endpoint manually:
```bash
# Create category
curl -X POST http://localhost:5109/api/spending/categories \
  -H "Content-Type: application/json" \
  -d '{"name":"Food","description":"Food expenses"}'

# List categories
curl http://localhost:5109/api/spending/categories

# Get monthly analytics
curl "http://localhost:5109/api/analytics/summary/monthly?year=2025&month=12"
```

## CI/CD Integration

Add to your CI pipeline:

```yaml
# Example GitHub Actions
- name: Run API Tests
  run: |
    docker-compose up -d
    dotnet ef database update
    dotnet run --project src/Api/SpendBear.Api &
    sleep 10
    ./scripts/test-api.sh
```

## Next Steps

After running tests:
1. Review any failures in the output
2. Check API logs for detailed error messages
3. Use Swagger UI for interactive testing: http://localhost:5109/scalar/v1
4. Add more test cases as needed
