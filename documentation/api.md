# API Design - SpendBear

## API Conventions

### Base URL
- Development: `https://localhost:7001/api`
- Staging: `https://spendbear-staging.azurewebsites.net/api`
- Production: `https://api.spendbear.com`

### Versioning
- Version in URL: `/api/v1/`
- Header versioning for minor changes: `X-API-Version: 1.1`

### Authentication
- Bearer token in Authorization header
- Token format: `Authorization: Bearer {jwt_token}`
- Token expiry: 1 hour
- Refresh token expiry: 7 days

### Response Format
```json
{
  "data": { },
  "meta": {
    "timestamp": "2024-11-29T12:00:00Z",
    "version": "1.0",
    "requestId": "550e8400-e29b-41d4-a716-446655440000"
  },
  "errors": []
}
```

### Error Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The amount must be greater than zero",
  "instance": "/api/v1/transactions",
  "errors": {
    "amount": ["Amount must be greater than zero"],
    "categoryId": ["Category not found"]
  },
  "traceId": "00-982607166a1c3f45892bf3f0d7d57820-4966e4842266874f-00"
}
```

### HTTP Status Codes
- `200 OK` - Successful GET, PUT
- `201 Created` - Successful POST
- `204 No Content` - Successful DELETE
- `400 Bad Request` - Validation errors
- `401 Unauthorized` - Missing/invalid token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Business rule violation
- `422 Unprocessable Entity` - Semantic errors
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error

## Identity Module Endpoints

### POST /api/v1/auth/register
Register new user after Auth0 authentication

**Request:**
```json
{
  "auth0UserId": "auth0|507f1f77bcf86cd799439011",
  "email": "user@example.com",
  "displayName": "John Doe",
  "currency": "USD",
  "locale": "en-US"
}
```

**Response (201):**
```json
{
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "displayName": "John Doe",
    "currency": "USD",
    "locale": "en-US"
  }
}
```

### GET /api/v1/users/profile
Get current user profile

**Response (200):**
```json
{
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "displayName": "John Doe",
    "currency": "USD",
    "locale": "en-US",
    "notificationPreferences": {
      "email": true,
      "push": true,
      "budgetAlerts": {
        "enabled": true,
        "thresholds": [50, 75, 90, 100]
      }
    },
    "createdAt": "2024-01-15T10:00:00Z"
  }
}
```

### PUT /api/v1/users/profile
Update user profile

**Request:**
```json
{
  "displayName": "Jane Doe",
  "currency": "EUR",
  "notificationPreferences": {
    "email": false,
    "budgetAlerts": {
      "thresholds": [75, 90]
    }
  }
}
```

## Spending Module Endpoints

### POST /api/v1/transactions
Create new transaction

**Request:**
```json
{
  "amount": 45.99,
  "currency": "USD",
  "date": "2024-11-29",
  "categoryId": "550e8400-e29b-41d4-a716-446655440001",
  "merchant": "Starbucks",
  "notes": "Morning coffee",
  "tags": ["coffee", "work"]
}
```

**Response (201):**
```json
{
  "data": {
    "transactionId": "550e8400-e29b-41d4-a716-446655440002",
    "amount": 45.99,
    "currency": "USD",
    "date": "2024-11-29",
    "category": {
      "id": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Food & Dining",
      "icon": "🍔"
    },
    "merchant": "Starbucks",
    "notes": "Morning coffee",
    "createdAt": "2024-11-29T12:00:00Z"
  }
}
```

### GET /api/v1/transactions
Get user transactions with filtering

**Query Parameters:**
- `startDate` - ISO date (2024-11-01)
- `endDate` - ISO date (2024-11-30)
- `categoryId` - UUID
- `minAmount` - decimal
- `maxAmount` - decimal
- `merchant` - string (partial match)
- `page` - int (default: 1)
- `pageSize` - int (default: 20, max: 100)
- `sortBy` - date|amount (default: date)
- `sortOrder` - asc|desc (default: desc)

**Response (200):**
```json
{
  "data": {
    "transactions": [
      {
        "transactionId": "550e8400-e29b-41d4-a716-446655440002",
        "amount": 45.99,
        "currency": "USD",
        "date": "2024-11-29",
        "category": {
          "name": "Food & Dining",
          "icon": "🍔"
        },
        "merchant": "Starbucks"
      }
    ],
    "pagination": {
      "page": 1,
      "pageSize": 20,
      "totalItems": 150,
      "totalPages": 8
    }
  }
}
```

### GET /api/v1/transactions/{id}
Get single transaction

### PUT /api/v1/transactions/{id}
Update transaction

### DELETE /api/v1/transactions/{id}
Delete transaction

### GET /api/v1/categories
Get all categories (user's + defaults)

**Response (200):**
```json
{
  "data": {
    "categories": [
      {
        "id": "550e8400-e29b-41d4-a716-446655440001",
        "name": "Food & Dining",
        "icon": "🍔",
        "color": "#FF6B6B",
        "isDefault": true,
        "isCustom": false
      },
      {
        "id": "550e8400-e29b-41d4-a716-446655440003",
        "name": "Vintage Vinyls",
        "icon": "💿",
        "color": "#4ECDC4",
        "isDefault": false,
        "isCustom": true
      }
    ]
  }
}
```

### POST /api/v1/categories
Create custom category

**Request:**
```json
{
  "name": "Vintage Vinyls",
  "icon": "💿",
  "color": "#4ECDC4"
}
```

### PUT /api/v1/categories/{id}
Update custom category

### DELETE /api/v1/categories/{id}
Delete custom category

## Budgets Module Endpoints

### POST /api/v1/budgets
Create budget

**Request:**
```json
{
  "name": "Monthly Food Budget",
  "amountLimit": 500.00,
  "periodType": "monthly",
  "startDate": "2024-11-01",
  "endDate": "2024-11-30",
  "categoryId": "550e8400-e29b-41d4-a716-446655440001"
}
```

**Response (201):**
```json
{
  "data": {
    "budgetId": "550e8400-e29b-41d4-a716-446655440004",
    "name": "Monthly Food Budget",
    "amountLimit": 500.00,
    "periodType": "monthly",
    "startDate": "2024-11-01",
    "endDate": "2024-11-30",
    "category": {
      "name": "Food & Dining",
      "icon": "🍔"
    },
    "currentSpend": 0,
    "remaining": 500.00,
    "percentageUsed": 0
  }
}
```

### GET /api/v1/budgets
Get all active budgets

**Query Parameters:**
- `active` - bool (default: true)
- `periodType` - monthly|weekly|custom
- `includeExpired` - bool (default: false)

**Response (200):**
```json
{
  "data": {
    "budgets": [
      {
        "budgetId": "550e8400-e29b-41d4-a716-446655440004",
        "name": "Monthly Food Budget",
        "amountLimit": 500.00,
        "currentSpend": 237.45,
        "remaining": 262.55,
        "percentageUsed": 47.49,
        "status": "ok",
        "daysRemaining": 5
      }
    ]
  }
}
```

### GET /api/v1/budgets/{id}
Get budget details with transaction breakdown

### PUT /api/v1/budgets/{id}
Update budget

### DELETE /api/v1/budgets/{id}
Delete budget

### GET /api/v1/budgets/{id}/status
Get real-time budget status

**Response (200):**
```json
{
  "data": {
    "budgetId": "550e8400-e29b-41d4-a716-446655440004",
    "amountLimit": 500.00,
    "currentSpend": 475.00,
    "remaining": 25.00,
    "percentageUsed": 95.00,
    "status": "critical",
    "projectedOverspend": 50.00,
    "averageDailySpend": 15.83,
    "lastTransaction": {
      "amount": 45.99,
      "date": "2024-11-29T10:30:00Z",
      "merchant": "Whole Foods"
    }
  }
}
```

## Analytics Module Endpoints

### GET /api/v1/analytics/summary
Get spending summary

**Query Parameters:**
- `period` - daily|weekly|monthly|yearly
- `startDate` - ISO date
- `endDate` - ISO date

**Response (200):**
```json
{
  "data": {
    "period": "monthly",
    "startDate": "2024-11-01",
    "endDate": "2024-11-30",
    "totalSpend": 2456.78,
    "transactionCount": 67,
    "averageTransaction": 36.67,
    "comparisonToPrevious": {
      "amount": -234.50,
      "percentage": -8.71
    },
    "byCategory": [
      {
        "category": "Food & Dining",
        "amount": 654.32,
        "percentage": 26.6,
        "transactionCount": 23
      },
      {
        "category": "Transportation",
        "amount": 432.10,
        "percentage": 17.6,
        "transactionCount": 12
      }
    ]
  }
}
```

### GET /api/v1/analytics/trends
Get spending trends

**Query Parameters:**
- `period` - 7d|30d|90d|1y
- `groupBy` - day|week|month

**Response (200):**
```json
{
  "data": {
    "period": "30d",
    "dataPoints": [
      {
        "date": "2024-11-01",
        "amount": 125.50,
        "transactionCount": 3
      },
      {
        "date": "2024-11-02",
        "amount": 89.20,
        "transactionCount": 2
      }
    ],
    "trendLine": "decreasing",
    "averageDaily": 81.89,
    "peakDay": {
      "date": "2024-11-15",
      "amount": 345.67
    }
  }
}
```

### GET /api/v1/analytics/insights
Get AI-powered insights (future)

### GET /api/v1/analytics/export
Export data as CSV/PDF

**Query Parameters:**
- `format` - csv|pdf|json
- `startDate` - ISO date
- `endDate` - ISO date

## Statement Import Module Endpoints

### POST /api/statement-import/upload
Upload a PDF bank statement for AI-powered parsing

**Request:** `multipart/form-data`
- `file` - PDF file (max 10MB, must be `.pdf` extension)

**Response (201):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440010",
  "originalFileName": "chase-november-2025.pdf",
  "uploadedAt": "2025-11-30T12:00:00Z",
  "status": "PendingReview",
  "errorMessage": null,
  "parsedTransactions": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440011",
      "date": "2025-11-15T00:00:00Z",
      "description": "Starbucks Coffee",
      "amount": 5.75,
      "currency": "USD",
      "suggestedCategoryId": "550e8400-e29b-41d4-a716-446655440001",
      "confirmedCategoryId": null,
      "effectiveCategoryId": "550e8400-e29b-41d4-a716-446655440001",
      "originalText": "STARBUCKS #12345 11/15"
    }
  ]
}
```

**Errors:**
- `400` - No file provided, not a PDF, or exceeds 10MB
- `422` - PDF extraction or AI parsing failed

**Notes:** The upload triggers an AI parsing pipeline: PDF text extraction (PdfPig) → OpenAI GPT-4o-mini categorization → PendingReview status. The `Location` header points to the GET endpoint for the created import.

### GET /api/statement-import/{id}
Get a specific statement import with parsed transactions

**Response (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440010",
  "originalFileName": "chase-november-2025.pdf",
  "uploadedAt": "2025-11-30T12:00:00Z",
  "status": "PendingReview",
  "errorMessage": null,
  "parsedTransactions": [...]
}
```

**Errors:**
- `404` - Import not found
- `403` - Not the owner of this import

### GET /api/statement-import
List all statement imports for the authenticated user

**Response (200):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440010",
    "originalFileName": "chase-november-2025.pdf",
    "uploadedAt": "2025-11-30T12:00:00Z",
    "status": "PendingReview",
    "transactionCount": 15
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440012",
    "originalFileName": "bofa-october-2025.pdf",
    "uploadedAt": "2025-10-30T12:00:00Z",
    "status": "Confirmed",
    "transactionCount": 23
  }
]
```

### PUT /api/statement-import/{id}/transactions
Update categories for parsed transactions before confirming

**Request:**
```json
{
  "updates": [
    {
      "parsedTransactionId": "550e8400-e29b-41d4-a716-446655440011",
      "newCategoryId": "550e8400-e29b-41d4-a716-446655440003"
    }
  ]
}
```

**Response (200):** Returns the updated `StatementUploadDto` (same shape as GET).

**Errors:**
- `404` - Import or transaction not found
- `409` - Import is not in PendingReview status

### POST /api/statement-import/{id}/confirm
Confirm the import and create transactions in the Spending module

**Response (204):** No Content

**Behavior:** Creates one transaction per parsed transaction in the Spending module using the effective category (confirmed or AI-suggested). Raises `StatementImportConfirmedEvent`. Status transitions to `Confirmed`.

**Errors:**
- `404` - Import not found
- `409` - Import is not in PendingReview status
- `422` - Transaction creation failed

### POST /api/statement-import/{id}/cancel
Cancel the import

**Response (204):** No Content

**Behavior:** Status transitions to `Cancelled`. No transactions are created.

**Errors:**
- `404` - Import not found
- `409` - Import is not in a cancellable status

## Webhook Endpoints

### POST /api/webhooks/auth0
Auth0 user events webhook

### POST /api/webhooks/bank-sync
Bank transaction sync webhook (note: Statement Import module now provides PDF-based import functionality)

## Rate Limiting

### Limits
- Anonymous: 10 requests/minute
- Authenticated: 100 requests/minute
- Premium: 1000 requests/minute

### Headers
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1701259200
```

## Pagination

### Request
```
GET /api/v1/transactions?page=2&pageSize=20
```

### Response Headers
```
X-Pagination-Current-Page: 2
X-Pagination-Page-Size: 20
X-Pagination-Total-Count: 150
X-Pagination-Total-Pages: 8
Link: </api/v1/transactions?page=3&pageSize=20>; rel="next",
      </api/v1/transactions?page=1&pageSize=20>; rel="prev",
      </api/v1/transactions?page=1&pageSize=20>; rel="first",
      </api/v1/transactions?page=8&pageSize=20>; rel="last"
```

## Filtering & Sorting

### Filtering Syntax
```
GET /api/v1/transactions?filter[amount][gte]=100&filter[amount][lte]=500
GET /api/v1/transactions?filter[category]=food,transport
GET /api/v1/transactions?filter[date][between]=2024-11-01,2024-11-30
```

### Sorting Syntax
```
GET /api/v1/transactions?sort=-date,amount  // Sort by date DESC, then amount ASC
```

## Field Selection

### Sparse Fieldsets
```
GET /api/v1/transactions?fields=amount,date,category
```

### Including Relations
```
GET /api/v1/transactions?include=category,tags
```

## Batch Operations

### POST /api/v1/transactions/batch
Create multiple transactions

**Request:**
```json
{
  "transactions": [
    {
      "amount": 45.99,
      "categoryId": "550e8400-e29b-41d4-a716-446655440001",
      "date": "2024-11-29"
    },
    {
      "amount": 12.50,
      "categoryId": "550e8400-e29b-41d4-a716-446655440002",
      "date": "2024-11-29"
    }
  ]
}
```

**Response (207 Multi-Status):**
```json
{
  "data": {
    "succeeded": 2,
    "failed": 0,
    "results": [
      {
        "status": 201,
        "transactionId": "550e8400-e29b-41d4-a716-446655440005"
      },
      {
        "status": 201,
        "transactionId": "550e8400-e29b-41d4-a716-446655440006"
      }
    ]
  }
}
```

## WebSocket Events (Future)

### Connection
```javascript
const ws = new WebSocket('wss://api.spendbear.com/ws');
ws.send(JSON.stringify({
  type: 'auth',
  token: 'Bearer {jwt_token}'
}));
```

### Events
```javascript
// Budget threshold reached
{
  "type": "budget.threshold",
  "data": {
    "budgetId": "550e8400-e29b-41d4-a716-446655440004",
    "percentage": 90,
    "remaining": 50.00
  }
}

// Transaction created
{
  "type": "transaction.created",
  "data": {
    "transactionId": "550e8400-e29b-41d4-a716-446655440007",
    "amount": 25.00
  }
}
```

## API Testing

### Health Check
```
GET /health
GET /health/ready
GET /health/live
```

### OpenAPI/Swagger
```
GET /swagger
GET /swagger/v1/swagger.json
```

## SDK Support (Future)

### JavaScript/TypeScript
```typescript
import { SpendBearClient } from '@spendbear/sdk';

const client = new SpendBearClient({
  apiKey: 'your_api_key',
  version: 'v1'
});

const transaction = await client.transactions.create({
  amount: 45.99,
  categoryId: 'food_dining'
});
```

### .NET
```csharp
var client = new SpendBearClient(apiKey);
var transaction = await client.Transactions.CreateAsync(new {
  Amount = 45.99m,
  CategoryId = Guid.Parse("...")
});
```

## Deprecation Policy

- Deprecation notice: 3 months minimum
- Sunset period: 6 months
- Deprecation header: `X-API-Deprecated: true`
- Sunset header: `X-API-Sunset: 2025-06-01`

## Security Headers

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Strict-Transport-Security: max-age=31536000; includeSubDomains
Content-Security-Policy: default-src 'self'
```

## CORS Configuration

```
Access-Control-Allow-Origin: https://spendbear.com
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type, Authorization
Access-Control-Max-Age: 86400
```
