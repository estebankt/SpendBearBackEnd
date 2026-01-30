#!/bin/bash

# Quick API Health Check
# Quickly tests if API is responding and basic endpoints work

API_URL="http://localhost:5109"

echo "🔍 Quick API Test"
echo "=================="
echo ""

# Test 1: API Health
echo "1. Checking API health..."
if curl -s -f -o /dev/null "$API_URL/scalar/v1"; then
    echo "   ✓ API is running"
else
    echo "   ✗ API is not responding"
    echo ""
    echo "Please start the API with:"
    echo "  dotnet run --project src/Api/SpendBear.Api"
    exit 1
fi

# Test 2: Create Category
echo "2. Creating test category..."
CATEGORY_RESPONSE=$(curl -s -X POST "$API_URL/api/spending/categories" \
    -H "Content-Type: application/json" \
    -d '{"name":"Test Category","description":"Quick test"}')

if echo "$CATEGORY_RESPONSE" | grep -q "id"; then
    CATEGORY_ID=$(echo "$CATEGORY_RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
    echo "   ✓ Category created: $CATEGORY_ID"
else
    echo "   ✗ Failed to create category"
    echo "   Response: $CATEGORY_RESPONSE"
    exit 1
fi

# Test 3: Get Categories
echo "3. Fetching categories..."
CATEGORIES=$(curl -s "$API_URL/api/spending/categories")
if echo "$CATEGORIES" | grep -q "Test Category"; then
    echo "   ✓ Categories retrieved"
else
    echo "   ✗ Failed to retrieve categories"
fi

# Test 4: Create Transaction
echo "4. Creating test transaction..."
TRANSACTION_RESPONSE=$(curl -s -X POST "$API_URL/api/spending/transactions" \
    -H "Content-Type: application/json" \
    -d "{
        \"amount\": 25.50,
        \"currency\": \"USD\",
        \"date\": \"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\",
        \"description\": \"Quick test transaction\",
        \"categoryId\": \"$CATEGORY_ID\",
        \"type\": \"Expense\"
    }")

if echo "$TRANSACTION_RESPONSE" | grep -q "id"; then
    echo "   ✓ Transaction created"
else
    echo "   ✗ Failed to create transaction"
    echo "   Response: $TRANSACTION_RESPONSE"
fi

# Test 5: Get Transactions
echo "5. Fetching transactions..."
TRANSACTIONS=$(curl -s "$API_URL/api/spending/transactions")
if echo "$TRANSACTIONS" | grep -q "Quick test"; then
    echo "   ✓ Transactions retrieved"
else
    echo "   ✗ Failed to retrieve transactions"
fi

# Test 6: Get Analytics
echo "6. Fetching analytics..."
YEAR=$(date +"%Y")
MONTH=$(date +"%m")
ANALYTICS=$(curl -s "$API_URL/api/analytics/summary/monthly?year=$YEAR&month=$MONTH")
if echo "$ANALYTICS" | grep -q "totalExpense"; then
    echo "   ✓ Analytics retrieved"
else
    echo "   ✗ Failed to retrieve analytics"
fi

echo ""
echo "✅ Quick test complete!"
echo ""
echo "Run full test suite with:"
echo "  ./scripts/test-api.sh"
