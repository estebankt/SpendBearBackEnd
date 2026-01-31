#!/bin/bash

# Cleanup Test Data Script
# Removes all data for the test user from the database

set -e

# Test user ID used by development middleware
TEST_USER_ID="00000000-0000-0000-0000-000000000001"

# Database connection (from docker-compose)
DB_HOST="localhost"
DB_PORT="5432"
DB_NAME="testdb"
DB_USER="testuser"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}SpendBear Test Data Cleanup${NC}"
echo "=============================="
echo ""
echo "This will delete all data for test user: $TEST_USER_ID"
echo ""
read -p "Continue? (y/N) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Cancelled."
    exit 0
fi

echo ""
echo "Connecting to PostgreSQL..."

# Use PGPASSWORD environment variable to avoid password prompt
export PGPASSWORD="testpass"

# Delete data in correct order (respecting foreign keys)
echo "Deleting test data..."

# Notifications (no dependencies)
echo -n "  - Notifications... "
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
    "DELETE FROM notifications.\"Notifications\" WHERE \"UserId\" = '$TEST_USER_ID';" > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

# Analytics (no dependencies)
echo -n "  - Analytics snapshots... "
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
    "DELETE FROM analytics.\"AnalyticSnapshots\" WHERE \"UserId\" = '$TEST_USER_ID';" > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

# Budgets (references categories)
echo -n "  - Budgets... "
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
    "DELETE FROM budgets.\"Budgets\" WHERE \"UserId\" = '$TEST_USER_ID';" > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

# Transactions (references categories)
echo -n "  - Transactions... "
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
    "DELETE FROM spending.\"Transactions\" WHERE \"UserId\" = '$TEST_USER_ID';" > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

# Categories (last, others depend on it)
echo -n "  - Categories... "
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
    "DELETE FROM public.categories WHERE \"UserId\" = '$TEST_USER_ID';" > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

# Identity user (if exists)
echo -n "  - Identity user... "
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c \
    "DELETE FROM identity.\"Users\" WHERE \"Id\" = '$TEST_USER_ID';" > /dev/null 2>&1
echo -e "${GREEN}✓${NC}"

echo ""
echo -e "${GREEN}✓ Test data cleaned successfully!${NC}"
echo ""
echo "You can now run fresh tests with:"
echo "  ./scripts/quick-test.sh"
echo "  ./scripts/test-api.sh"
