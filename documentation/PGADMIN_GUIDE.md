# pgAdmin Setup Guide

Quick guide for accessing and using pgAdmin to manage your SpendBear PostgreSQL database.

## Starting pgAdmin

### 1. Start Docker Compose
```bash
docker-compose up -d
```

This will start:
- PostgreSQL (port 5432)
- Redis (port 6379)
- pgAdmin (port 5050)
- API (ports 5109, 7036)

### 2. Access pgAdmin
Open your browser and navigate to:
```
http://localhost:5050
```

### 3. Login Credentials
```
Email: admin@spendbear.com
Password: admin
```

## First Time Setup

### Connect to SpendBear Database

1. **Click "Add New Server"** (or right-click "Servers" → "Register" → "Server")

2. **General Tab:**
   - Name: `SpendBear Local`

3. **Connection Tab:**
   - Host name/address: `postgres` (Docker service name)
   - Port: `5432`
   - Maintenance database: `spendbear`
   - Username: `postgres`
   - Password: `postgres`
   - Save password: ✅ (checked)

4. **Click "Save"**

You should now see the SpendBear database connected!

## Database Structure

### Schemas

SpendBear uses separate schemas for each module:

```
spendbear (database)
├── public
│   └── categories (shared across modules)
├── identity
│   └── Users
├── spending
│   └── Transactions
├── budgets
│   └── Budgets
├── notifications
│   └── Notifications
└── analytics
    └── AnalyticSnapshots
```

## Common Tasks

### View All Tables

1. Expand: **Servers** → **SpendBear Local** → **Databases** → **spendbear** → **Schemas**
2. Expand each schema (public, identity, spending, budgets, notifications, analytics)
3. Expand **Tables** under each schema

### Query Data

1. Right-click on **spendbear** database
2. Select **Query Tool**
3. Write your SQL query:

```sql
-- View all transactions
SELECT * FROM spending."Transactions";

-- View all budgets
SELECT * FROM budgets."Budgets";

-- View all categories
SELECT * FROM public.categories;

-- View analytics snapshots
SELECT * FROM analytics."AnalyticSnapshots";

-- View notifications
SELECT * FROM notifications."Notifications";

-- Get monthly summary
SELECT
    "Year",
    "Month",
    "TotalIncome",
    "TotalExpense",
    "NetBalance"
FROM analytics."AnalyticSnapshots"
WHERE "Period" = 'Monthly'
ORDER BY "Year" DESC, "Month" DESC;
```

4. Press **F5** or click the **Execute** button (▶️)

### View Table Data (GUI)

1. Navigate to the table (e.g., `spending` → `Tables` → `Transactions`)
2. Right-click on the table
3. Select **View/Edit Data** → **All Rows**

### Filter Data

In the data view:
1. Click the **Filter** icon (funnel)
2. Enter your filter criteria
3. Example: `UserId = '00000000-0000-0000-0000-000000000001'`

### Export Data

1. View the data you want to export
2. Click **Download** icon
3. Choose format (CSV, JSON, etc.)

### Run Migrations Status

```sql
-- Check which migrations have been applied
SELECT * FROM identity."__EFMigrationsHistory";
SELECT * FROM spending."__EFMigrationsHistory";
SELECT * FROM budgets."__EFMigrationsHistory";
SELECT * FROM notifications."__EFMigrationsHistory";
SELECT * FROM analytics."__EFMigrationsHistory";
```

### Clean Test Data

```sql
-- Delete all data for test user
DELETE FROM spending."Transactions" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM budgets."Budgets" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM notifications."Notifications" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM analytics."AnalyticSnapshots" WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM public.categories WHERE "UserId" = '00000000-0000-0000-0000-000000000001';
DELETE FROM identity."Users" WHERE "Id" = '00000000-0000-0000-0000-000000000001';
```

Or use the bash script:
```bash
./scripts/cleanup-test-data.sh
```

### View Indexes

1. Navigate to the table
2. Expand the table
3. Click on **Indexes**

### View Foreign Keys

1. Navigate to the table
2. Expand the table
3. Click on **Constraints** → **Foreign Keys**

## Useful Queries

### User Activity Summary

```sql
SELECT
    u."Email",
    COUNT(DISTINCT t."Id") as transaction_count,
    COUNT(DISTINCT b."Id") as budget_count,
    COUNT(DISTINCT n."Id") as notification_count
FROM identity."Users" u
LEFT JOIN spending."Transactions" t ON u."Id" = t."UserId"
LEFT JOIN budgets."Budgets" b ON u."Id" = b."UserId"
LEFT JOIN notifications."Notifications" n ON u."Id" = n."UserId"
GROUP BY u."Email";
```

### Budget vs Actual Spending

```sql
SELECT
    b."Name" as budget_name,
    b."Amount" as budget_amount,
    b."CurrentAmount" as spent,
    b."Amount" - b."CurrentAmount" as remaining,
    ROUND((b."CurrentAmount" / b."Amount" * 100)::numeric, 2) as percentage_used
FROM budgets."Budgets" b
WHERE b."Period" = 'Monthly'
ORDER BY percentage_used DESC;
```

### Recent Transactions

```sql
SELECT
    t."Date",
    t."Description",
    t."Amount",
    t."Currency",
    t."Type",
    c."Name" as category
FROM spending."Transactions" t
JOIN public.categories c ON t."CategoryId" = c."Id"
ORDER BY t."Date" DESC
LIMIT 20;
```

### Notification Status

```sql
SELECT
    "Type",
    "Status",
    COUNT(*) as count
FROM notifications."Notifications"
GROUP BY "Type", "Status"
ORDER BY "Type", "Status";
```

## Tips & Tricks

### Auto-Refresh
- Enable auto-refresh for live data updates
- Dashboard → Preferences → SQL Editor → Auto-refresh query results

### Keyboard Shortcuts
- `F5` - Execute query
- `Ctrl + Space` - Auto-complete
- `Ctrl + Shift + F` - Format SQL
- `Ctrl + /` - Comment/uncomment line

### Save Queries
1. Write your query
2. Click the **Save** icon (💾)
3. Give it a name
4. Access from **Files** → **Saved Queries**

### ERD Diagram
1. Right-click on **spendbear** database
2. Select **Generate ERD**
3. Visual representation of your database structure

### Backup Database
1. Right-click on **spendbear** database
2. Select **Backup...**
3. Choose format (Custom, Tar, Plain)
4. Select file location
5. Click **Backup**

### Restore Database
1. Right-click on **spendbear** database
2. Select **Restore...**
3. Choose backup file
4. Click **Restore**

## Troubleshooting

### Can't Connect to Database

**Problem:** Connection refused or timeout

**Solutions:**
```bash
# 1. Check containers are running
docker-compose ps

# 2. Check postgres is healthy
docker-compose logs postgres

# 3. Restart services
docker-compose restart postgres pgadmin

# 4. Verify connection from host
psql -h localhost -U postgres -d spendbear
```

### "Server closed the connection unexpectedly"

**Solution:** Restart PostgreSQL
```bash
docker-compose restart postgres
```

### pgAdmin Shows Empty Database

**Problem:** No schemas visible

**Solution:**
1. Check connection settings (use `postgres` not `localhost` for host)
2. Ensure migrations have been run
3. Refresh the database (right-click → Refresh)

### Slow Queries

**Solutions:**
1. Add indexes to frequently queried columns
2. Use `EXPLAIN ANALYZE` to see query plan:
   ```sql
   EXPLAIN ANALYZE SELECT * FROM spending."Transactions" WHERE "UserId" = 'xxx';
   ```
3. Check database stats:
   ```sql
   SELECT * FROM pg_stat_user_tables;
   ```

## Security Notes

⚠️ **Development Only**

The current pgAdmin setup is configured for **local development only**:
- Default credentials (admin/admin)
- No authentication required
- Server mode disabled

**For production:**
1. Use strong passwords
2. Enable server mode
3. Configure proper authentication
4. Use SSL connections
5. Restrict network access

## Docker Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs pgadmin
docker-compose logs postgres

# Restart pgAdmin
docker-compose restart pgadmin

# Remove all containers and volumes (⚠️ deletes data!)
docker-compose down -v
```

## Resources

- [pgAdmin Documentation](https://www.pgadmin.org/docs/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [SQL Tutorial](https://www.postgresql.org/docs/current/tutorial.html)

---

**Access URL:** http://localhost:5050
**Username:** admin@spendbear.com
**Password:** admin

**Database Connection:**
- Host: `postgres`
- Port: `5432`
- Database: `spendbear`
- Username: `postgres`
- Password: `postgres`
