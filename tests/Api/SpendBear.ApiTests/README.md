# SpendBear API Tests

Comprehensive API tests using `WebApplicationFactory` and TestContainers for end-to-end testing of the SpendBear API.

## Overview

These tests verify the **complete HTTP request/response cycle** including:
- HTTP routing and middleware
- Authentication (development mode)
- Request serialization/deserialization
- Controller logic
- Application handlers
- Database persistence
- Event-driven integration across modules

## Test Structure

### Test Classes

| Test Class | Focus | Tests |
|------------|-------|-------|
| `SpendingModuleApiTests` | Spending endpoints | 8 tests - Categories & Transactions CRUD |
| `BudgetsModuleApiTests` | Budget endpoints | 7 tests - Budgets CRUD + validation |
| `AnalyticsModuleApiTests` | Analytics endpoints | 4 tests - Monthly summaries + event integration |
| `EndToEndWorkflowTests` | Cross-module workflows | 5 tests - Event flows across all modules |

**Total:** 24 API tests

### Test Base Class

`ApiTestBase.cs` provides:
- PostgreSQL TestContainer setup (fresh database per test class)
- `WebApplicationFactory<Program>` configuration
- Automatic database migrations for all 5 modules
- Event dispatcher registration
- HttpClient for API calls

## Test Categories

### 1. CRUD Operations
Tests basic create, read, update, delete for each module:
```csharp
[Fact]
public async Task CreateCategory_WithValidData_ReturnsCreated()
{
    var request = new { name = "Food", description = "..." };
    var response = await Client.PostAsJsonAsync("/api/spending/categories", request);
    response.StatusCode.Should().Be(HttpStatusCode.Created);
}
```

### 2. Validation Tests
Tests invalid input handling:
```csharp
[Fact]
public async Task CreateTransaction_WithInvalidAmount_ReturnsBadRequest()
{
    var invalidRequest = new { amount = -50.00m, ... }; // Negative amount
    var response = await Client.PostAsJsonAsync("/api/spending/transactions", invalidRequest);
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

### 3. Event Flow Tests
Tests asynchronous event processing across modules:
```csharp
[Fact]
public async Task CreateTransaction_TriggersAnalyticsUpdate()
{
    // Create transaction
    await Client.PostAsJsonAsync("/api/spending/transactions", transaction);

    // Wait for async event processing
    await Task.Delay(500);

    // Verify analytics snapshot created
    var analytics = await Client.GetAsync("/api/analytics/summary/monthly");
    analytics.Should().ContainData();
}
```

### 4. End-to-End Workflows
Tests complete user scenarios:
- Create category → Create transaction → Verify budget updated → Verify analytics updated
- Exceed budget threshold → Verify notification created
- Multiple transactions → Verify aggregation in analytics

## Running the Tests

### Run All API Tests
```bash
dotnet test tests/Api/SpendBear.ApiTests
```

### Run Specific Test Class
```bash
dotnet test tests/Api/SpendBear.ApiTests --filter "FullyQualifiedName~SpendingModuleApiTests"
```

### Run with Detailed Output
```bash
dotnet test tests/Api/SpendBear.ApiTests --logger "console;verbosity=detailed"
```

### Run Single Test
```bash
dotnet test tests/Api/SpendBear.ApiTests --filter "FullyQualifiedName~CreateCategory_WithValidData_ReturnsCreated"
```

## Test Execution

### Test Isolation
- Each test class gets a **fresh PostgreSQL container**
- Tests within a class share the same database (fast)
- Tests run **sequentially** to avoid Serilog conflicts
- All data is cleaned up after test class completes

### Test Timeline
```
1. Test Class starts
   ├─ TestContainer starts PostgreSQL (2-3 seconds)
   ├─ Apply all 6 migrations (1-2 seconds)
   └─ Create HttpClient
2. Tests run (sequential)
   ├─ Test 1: HTTP request → API → Database → Assertions
   ├─ Test 2: HTTP request → API → Database → Assertions
   └─ Test N...
3. Test Class ends
   ├─ Dispose HttpClient
   ├─ Dispose WebApplicationFactory
   └─ Stop and remove PostgreSQL container
```

**Average execution time:** ~30-60 seconds for all 24 tests

## Integration with CI/CD

These tests are ideal for CI/CD pipelines:

### GitHub Actions Example
```yaml
- name: Run API Tests
  run: |
    docker pull postgres:16-alpine
    dotnet test tests/Api/SpendBear.ApiTests --logger "trx;LogFileName=api-test-results.trx"
```

### Azure DevOps Example
```yaml
- task: DotNetCoreCLI@2
  displayName: 'Run API Tests'
  inputs:
    command: test
    projects: 'tests/Api/SpendBear.ApiTests/*.csproj'
    arguments: '--logger trx --collect:"XPlat Code Coverage"'
```

## Debugging Tests

### View Test Output
```bash
dotnet test tests/Api/SpendBear.ApiTests --logger "console;verbosity=normal"
```

### Debug in IDE
1. Set breakpoint in test method
2. Right-click test → Debug Test
3. Inspect HTTP requests/responses
4. Check database state via test container connection

### Check Database State
While test is paused at breakpoint:
```csharp
// Get connection string from container
var connectionString = _postgresContainer.GetConnectionString();
// Use with psql or any DB tool to inspect data
```

## Comparison with Other Test Types

| Feature | Unit Tests | Integration Tests | **API Tests** | Bash Scripts |
|---------|-----------|-------------------|---------------|--------------|
| **Speed** | ⚡ Fast (ms) | 🚀 Medium (seconds) | **🐢 Slower (seconds)** | 🐌 Slowest |
| **Scope** | Single class | Cross-layer | **Full stack** | Black-box |
| **DB Required** | ❌ No | ✅ TestContainer | **✅ TestContainer** | ✅ Real DB |
| **HTTP Layer** | ❌ No | ❌ No | **✅ Yes** | ✅ Yes |
| **Auth Testing** | ❌ No | Partial | **✅ Full middleware** | ✅ Full |
| **CI/CD** | ✅ `dotnet test` | ✅ `dotnet test` | **✅ `dotnet test`** | ⚠️ Custom script |
| **Debugging** | ✅ Easy | ✅ Easy | **✅ Easy** | ❌ Hard |
| **Coverage** | Code paths | Event flows | **User scenarios** | E2E workflows |

## Best Practices

### ✅ Do
- Test happy paths AND error cases
- Use descriptive test names (`CreateCategory_WithValidData_ReturnsCreated`)
- Use FluentAssertions for readable assertions
- Wait for async events with `Task.Delay()`
- Test cross-module integration
- Verify HTTP status codes
- Check response data structure

### ❌ Don't
- Don't test business logic details (that's for unit tests)
- Don't make tests dependent on execution order
- Don't use hard-coded GUIDs (generate dynamically)
- Don't skip cleanup (TestContainer handles it)
- Don't test Auth0 integration (that's manual/E2E)

## Adding New Tests

### 1. Add test method to existing class
```csharp
[Fact]
public async Task YourTest_WithSomeCondition_HasExpectedOutcome()
{
    // Arrange
    var request = new { ... };

    // Act
    var response = await Client.PostAsJsonAsync("/api/endpoint", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<YourDto>();
    result.Should().NotBeNull();
}
```

### 2. Create new test class
```csharp
[Collection("API Tests")] // Important: Use same collection!
public class YourModuleApiTests : ApiTestBase
{
    [Fact]
    public async Task YourTest() { ... }
}
```

### 3. Add response DTOs
```csharp
// At bottom of test class
private record YourDto(Guid Id, string Name, ...);
```

## Troubleshooting

### Tests Fail to Start
- **Check Docker is running:** `docker ps`
- **Check port 5432 not in use:** `lsof -i :5432`
- **Pull postgres image:** `docker pull postgres:16-alpine`

### Serilog Logger Errors
- Tests should run sequentially (already configured)
- Check `[Collection("API Tests")]` attribute is present
- Logging is disabled in test configuration

### Event Processing Failures
- Increase `Task.Delay()` duration (500ms → 1000ms)
- Check event dispatcher is registered in `ApiTestBase`
- Verify domain events are being raised

### Database Migration Issues
- Check all 5 modules have migrations
- Verify connection string is passed correctly
- Check schema names match (`spending`, `budgets`, etc.)

## Coverage

These API tests complement your existing test suite:

| Test Type | Current Coverage |
|-----------|------------------|
| Unit Tests (Domain) | 91 tests ✅ |
| Integration Tests (Events) | 3 tests ✅ |
| **API Tests (HTTP)** | **24 tests** ✅ |
| **Total** | **118 tests** |

## Future Enhancements

- [ ] Add Notifications endpoint tests (mark as read, filtering)
- [ ] Add Identity module tests (when real auth flow implemented)
- [ ] Add performance tests (response time assertions)
- [ ] Add concurrent request tests
- [ ] Add bulk operation tests (CSV import, etc.)
- [ ] Generate test coverage reports
- [ ] Add API contract tests (schema validation)

## Resources

- [WebApplicationFactory Docs](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [TestContainers .NET Docs](https://dotnet.testcontainers.org/)
- [FluentAssertions Docs](https://fluentassertions.com/)
- [xUnit Docs](https://xunit.net/)

---

**Created:** 2025-12-01
**Total Tests:** 24
**Test Type:** API/E2E
**Framework:** xUnit + WebApplicationFactory + TestContainers
