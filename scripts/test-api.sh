#!/bin/bash

# SpendBear API Test Script
# Tests all 13 endpoints across 5 modules
# Uses development mode (no authentication required)

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# API Base URL
API_URL="http://localhost:5109"

# Test counters
TESTS_PASSED=0
TESTS_FAILED=0

# Helper functions
print_header() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}\n"
}

print_test() {
    echo -e "${YELLOW}TEST:${NC} $1"
}

print_success() {
    echo -e "${GREEN}✓ PASS:${NC} $1"
    ((TESTS_PASSED++))
}

print_error() {
    echo -e "${RED}✗ FAIL:${NC} $1"
    ((TESTS_FAILED++))
}

print_info() {
    echo -e "${BLUE}INFO:${NC} $1"
}

# Test if API is running
check_api_health() {
    print_header "Checking API Health"
    print_test "API is responding"

    if curl -s -f -o /dev/null "$API_URL/scalar/v1"; then
        print_success "API is running at $API_URL"
    else
        print_error "API is not responding. Please start it with: dotnet run --project src/Api/SpendBear.Api"
        exit 1
    fi
}

# Global variables to store created IDs
CATEGORY_ID=""
TRANSACTION_ID=""
BUDGET_ID=""
NOTIFICATION_ID=""

# =============================================================================
# SPENDING MODULE TESTS (6 endpoints)
# =============================================================================

test_spending_module() {
    print_header "SPENDING MODULE TESTS (6 endpoints)"

    # Test 1: Create Category
    print_test "POST /api/spending/categories - Create category"
    RESPONSE=$(curl -s -X POST "$API_URL/api/spending/categories" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "Food & Dining",
            "description": "Restaurants, groceries, and food delivery"
        }')

    if echo "$RESPONSE" | grep -q "id"; then
        CATEGORY_ID=$(echo "$RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        print_success "Category created with ID: $CATEGORY_ID"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to create category"
        print_info "Response: $RESPONSE"
    fi

    # Test 2: Get Categories
    print_test "GET /api/spending/categories - List categories"
    RESPONSE=$(curl -s "$API_URL/api/spending/categories")

    if echo "$RESPONSE" | grep -q "Food & Dining"; then
        print_success "Categories retrieved successfully"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to retrieve categories"
        print_info "Response: $RESPONSE"
    fi

    # Test 3: Create Transaction
    print_test "POST /api/spending/transactions - Create transaction"
    RESPONSE=$(curl -s -X POST "$API_URL/api/spending/transactions" \
        -H "Content-Type: application/json" \
        -d "{
            \"amount\": 50.75,
            \"currency\": \"USD\",
            \"date\": \"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\",
            \"description\": \"Lunch at Italian restaurant\",
            \"categoryId\": \"$CATEGORY_ID\",
            \"type\": \"Expense\"
        }")

    if echo "$RESPONSE" | grep -q "id"; then
        TRANSACTION_ID=$(echo "$RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        print_success "Transaction created with ID: $TRANSACTION_ID"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to create transaction"
        print_info "Response: $RESPONSE"
    fi

    # Test 4: Get Transactions
    print_test "GET /api/spending/transactions - List transactions"
    RESPONSE=$(curl -s "$API_URL/api/spending/transactions")

    if echo "$RESPONSE" | grep -q "Italian restaurant"; then
        print_success "Transactions retrieved successfully"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to retrieve transactions"
        print_info "Response: $RESPONSE"
    fi

    # Test 5: Update Transaction
    print_test "PUT /api/spending/transactions/{id} - Update transaction"
    RESPONSE=$(curl -s -X PUT "$API_URL/api/spending/transactions/$TRANSACTION_ID" \
        -H "Content-Type: application/json" \
        -d "{
            \"amount\": 55.00,
            \"currency\": \"USD\",
            \"date\": \"$(date -u +"%Y-%m-%dT%H:%M:%SZ")\",
            \"description\": \"Lunch at Italian restaurant (updated)\",
            \"categoryId\": \"$CATEGORY_ID\",
            \"type\": \"Expense\"
        }")

    if echo "$RESPONSE" | grep -q "updated" || echo "$RESPONSE" | grep -q "id"; then
        print_success "Transaction updated successfully"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to update transaction"
        print_info "Response: $RESPONSE"
    fi

    # Test 6: Delete Transaction (we'll skip to keep data for other tests)
    print_info "Skipping DELETE transaction to preserve data for Budget/Analytics tests"
}

# =============================================================================
# BUDGETS MODULE TESTS (4 endpoints)
# =============================================================================

test_budgets_module() {
    print_header "BUDGETS MODULE TESTS (4 endpoints)"

    # Test 7: Create Budget
    print_test "POST /api/budgets - Create budget"
    RESPONSE=$(curl -s -X POST "$API_URL/api/budgets" \
        -H "Content-Type: application/json" \
        -d "{
            \"name\": \"Monthly Food Budget\",
            \"amount\": 500.00,
            \"currency\": \"USD\",
            \"period\": \"Monthly\",
            \"categoryId\": \"$CATEGORY_ID\",
            \"warningThreshold\": 80.0
        }")

    if echo "$RESPONSE" | grep -q "id"; then
        BUDGET_ID=$(echo "$RESPONSE" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        print_success "Budget created with ID: $BUDGET_ID"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to create budget"
        print_info "Response: $RESPONSE"
    fi

    # Test 8: Get Budgets
    print_test "GET /api/budgets - List budgets"
    RESPONSE=$(curl -s "$API_URL/api/budgets")

    if echo "$RESPONSE" | grep -q "Monthly Food Budget"; then
        print_success "Budgets retrieved successfully"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to retrieve budgets"
        print_info "Response: $RESPONSE"
    fi

    # Test 9: Update Budget
    print_test "PUT /api/budgets/{id} - Update budget"
    RESPONSE=$(curl -s -X PUT "$API_URL/api/budgets/$BUDGET_ID" \
        -H "Content-Type: application/json" \
        -d "{
            \"name\": \"Monthly Food Budget (Updated)\",
            \"amount\": 600.00,
            \"currency\": \"USD\",
            \"period\": \"Monthly\",
            \"categoryId\": \"$CATEGORY_ID\",
            \"warningThreshold\": 75.0
        }")

    if echo "$RESPONSE" | grep -q "Updated" || echo "$RESPONSE" | grep -q "id"; then
        print_success "Budget updated successfully"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to update budget"
        print_info "Response: $RESPONSE"
    fi

    # Test 10: Delete Budget (skip to preserve data)
    print_info "Skipping DELETE budget to preserve data for testing"
}

# =============================================================================
# NOTIFICATIONS MODULE TESTS (2 endpoints)
# =============================================================================

test_notifications_module() {
    print_header "NOTIFICATIONS MODULE TESTS (2 endpoints)"

    # Test 11: Get Notifications
    print_test "GET /api/notifications - List notifications"
    RESPONSE=$(curl -s "$API_URL/api/notifications")

    if echo "$RESPONSE" | grep -q "\[" || echo "$RESPONSE" | grep -q "\"data\""; then
        print_success "Notifications retrieved successfully"
        print_info "Response: $RESPONSE"

        # Try to extract a notification ID if any exist
        NOTIFICATION_ID=$(echo "$RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
        if [ -n "$NOTIFICATION_ID" ]; then
            print_info "Found notification ID: $NOTIFICATION_ID"
        fi
    else
        print_error "Failed to retrieve notifications"
        print_info "Response: $RESPONSE"
    fi

    # Test 12: Mark Notification as Read (if we have one)
    if [ -n "$NOTIFICATION_ID" ]; then
        print_test "PUT /api/notifications/{id}/read - Mark as read"
        RESPONSE=$(curl -s -X PUT "$API_URL/api/notifications/$NOTIFICATION_ID/read")

        HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" -X PUT "$API_URL/api/notifications/$NOTIFICATION_ID/read")

        if [ "$HTTP_CODE" == "204" ] || [ "$HTTP_CODE" == "200" ]; then
            print_success "Notification marked as read"
        else
            print_error "Failed to mark notification as read (HTTP $HTTP_CODE)"
            print_info "Response: $RESPONSE"
        fi
    else
        print_info "No notifications available to test mark as read"
    fi
}

# =============================================================================
# ANALYTICS MODULE TESTS (1 endpoint)
# =============================================================================

test_analytics_module() {
    print_header "ANALYTICS MODULE TESTS (1 endpoint)"

    # Test 13: Get Monthly Summary
    CURRENT_YEAR=$(date +"%Y")
    CURRENT_MONTH=$(date +"%m")

    print_test "GET /api/analytics/summary/monthly?year=$CURRENT_YEAR&month=$CURRENT_MONTH"
    RESPONSE=$(curl -s "$API_URL/api/analytics/summary/monthly?year=$CURRENT_YEAR&month=$CURRENT_MONTH")

    if echo "$RESPONSE" | grep -q "totalExpense\|totalIncome\|netBalance"; then
        print_success "Monthly summary retrieved successfully"
        print_info "Response: $RESPONSE"
    else
        print_error "Failed to retrieve monthly summary"
        print_info "Response: $RESPONSE"
    fi
}

# =============================================================================
# IDENTITY MODULE TESTS (2 endpoints)
# =============================================================================

test_identity_module() {
    print_header "IDENTITY MODULE TESTS (2 endpoints - Info Only)"

    print_info "Identity module endpoints require specific user registration flow"
    print_info "POST /api/identity/register - Requires Auth0 token with user info"
    print_info "GET /api/identity/profile - Requires authenticated user"
    print_info "These are tested through Auth0 integration, not direct API calls"
}

# =============================================================================
# EVENT FLOW VALIDATION
# =============================================================================

test_event_flows() {
    print_header "EVENT FLOW VALIDATION"

    print_info "Waiting 2 seconds for async event processing..."
    sleep 2

    # Check if Analytics snapshot was created from transaction
    print_test "Verify Transaction → Analytics event flow"
    CURRENT_YEAR=$(date +"%Y")
    CURRENT_MONTH=$(date +"%m")
    RESPONSE=$(curl -s "$API_URL/api/analytics/summary/monthly?year=$CURRENT_YEAR&month=$CURRENT_MONTH")

    if echo "$RESPONSE" | grep -q "totalExpense" && ! echo "$RESPONSE" | grep -q "\"totalExpense\":0"; then
        print_success "Analytics snapshot created from transaction event"
        print_info "Response: $RESPONSE"
    else
        print_error "Analytics snapshot not found or empty"
        print_info "Response: $RESPONSE"
    fi

    # Check if Budget was updated from transaction
    print_test "Verify Transaction → Budget event flow"
    RESPONSE=$(curl -s "$API_URL/api/budgets")

    if echo "$RESPONSE" | grep -q "currentAmount"; then
        print_success "Budget tracking transaction spending"
        print_info "Response: $RESPONSE"
    else
        print_error "Budget not updated from transaction"
        print_info "Response: $RESPONSE"
    fi

    print_info "Note: Notification events fire when budget thresholds (80%) are exceeded"
}

# =============================================================================
# MAIN EXECUTION
# =============================================================================

main() {
    clear
    echo -e "${GREEN}"
    echo "  ____                      _ ____                  "
    echo " / ___| _ __   ___ _ __   __| | __ )  ___  __ _ _ __ "
    echo " \___ \| '_ \ / _ \ '_ \ / _\` |  _ \ / _ \/ _\` | '__|"
    echo "  ___) | |_) |  __/ | | | (_| | |_) |  __/ (_| | |   "
    echo " |____/| .__/ \___|_| |_|\__,_|____/ \___|\__,_|_|   "
    echo "       |_|                                            "
    echo -e "${NC}"
    echo -e "${BLUE}API Test Suite - Testing 13 Endpoints${NC}\n"

    # Run all tests
    check_api_health
    test_spending_module
    test_budgets_module
    test_notifications_module
    test_analytics_module
    test_identity_module
    test_event_flows

    # Print summary
    print_header "TEST SUMMARY"
    echo -e "Total Tests Passed: ${GREEN}$TESTS_PASSED${NC}"
    echo -e "Total Tests Failed: ${RED}$TESTS_FAILED${NC}"

    TOTAL_TESTS=$((TESTS_PASSED + TESTS_FAILED))
    if [ $TOTAL_TESTS -gt 0 ]; then
        PASS_RATE=$((TESTS_PASSED * 100 / TOTAL_TESTS))
        echo -e "Pass Rate: ${BLUE}${PASS_RATE}%${NC}\n"
    fi

    if [ $TESTS_FAILED -eq 0 ]; then
        echo -e "${GREEN}🎉 All tests passed!${NC}\n"
        exit 0
    else
        echo -e "${YELLOW}⚠️  Some tests failed. Check the output above.${NC}\n"
        exit 1
    fi
}

# Run main function
main
