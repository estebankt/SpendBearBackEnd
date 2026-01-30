using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Spending.Domain.Entities;

namespace SpendBear.ApiTests;

/// <summary>
/// API tests for Spending Module endpoints.
/// Tests the full HTTP request/response cycle including routing, authentication, serialization, and business logic.
/// </summary>
[Collection("API Tests")]
public class SpendingModuleApiTests : ApiTestBase
{
    [Fact]
    public async Task CreateCategory_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            name = "Food & Dining",
            description = "Restaurants, groceries, and food delivery"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/spending/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        category.Should().NotBeNull();
        category!.Id.Should().NotBeEmpty();
        category.Name.Should().Be("Food & Dining");
        category.Description.Should().Be("Restaurants, groceries, and food delivery");
    }

    [Fact]
    public async Task GetCategories_AfterCreatingCategory_ReturnsCategories()
    {
        // Arrange - Create a category first
        var createRequest = new { name = "Transportation", description = "Gas, parking, public transit" };
        await Client.PostAsJsonAsync("/api/spending/categories", createRequest);

        // Act
        var response = await Client.GetAsync("/api/spending/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        categories.Should().NotBeNull();
        categories.Should().ContainSingle(c => c.Name == "Transportation");
    }

    [Fact]
    public async Task CreateTransaction_WithValidData_ReturnsCreated()
    {
        // Arrange - Create category first
        var categoryRequest = new { name = "Food", description = "Food expenses" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var transactionRequest = new
        {
            amount = 50.75m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Lunch at Italian restaurant",
            categoryId = category!.Id,
            type = "Expense"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/spending/transactions", transactionRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        transaction.Should().NotBeNull();
        transaction!.Id.Should().NotBeEmpty();
        transaction.Amount.Should().Be(50.75m);
        transaction.Currency.Should().Be("USD");
        transaction.Description.Should().Be("Lunch at Italian restaurant");
        transaction.Type.Should().Be("Expense");
    }

    [Fact]
    public async Task GetTransactions_AfterCreatingTransaction_ReturnsTransactions()
    {
        // Arrange - Create category and transaction
        var categoryRequest = new { name = "Shopping", description = "Retail purchases" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var transactionRequest = new
        {
            amount = 99.99m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "New shoes",
            categoryId = category!.Id,
            type = "Expense"
        };
        await Client.PostAsJsonAsync("/api/spending/transactions", transactionRequest);

        // Act
        var response = await Client.GetAsync("/api/spending/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        transactions.Should().NotBeNull();
        transactions.Should().Contain(t => t.Description == "New shoes");
    }

    [Fact]
    public async Task UpdateTransaction_WithValidData_ReturnsOk()
    {
        // Arrange - Create category and transaction
        var categoryRequest = new { name = "Entertainment", description = "Movies, games" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var createRequest = new
        {
            amount = 25.00m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Movie tickets",
            categoryId = category!.Id,
            type = "Expense"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/spending/transactions", createRequest);
        var transaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        var updateRequest = new
        {
            amount = 30.00m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Movie tickets with popcorn",
            categoryId = category.Id,
            type = "Expense"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/spending/transactions/{transaction!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        updated.Should().NotBeNull();
        updated!.Amount.Should().Be(30.00m);
        updated.Description.Should().Be("Movie tickets with popcorn");
    }

    [Fact]
    public async Task DeleteTransaction_WithValidId_ReturnsNoContent()
    {
        // Arrange - Create category and transaction
        var categoryRequest = new { name = "Health", description = "Medical expenses" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var createRequest = new
        {
            amount = 15.00m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Pharmacy",
            categoryId = category!.Id,
            type = "Expense"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/spending/transactions", createRequest);
        var transaction = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/spending/transactions/{transaction!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync("/api/spending/transactions");
        var transactions = await getResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        transactions.Should().NotContain(t => t.Id == transaction.Id);
    }

    [Fact]
    public async Task CreateTransaction_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var categoryRequest = new { name = "Test", description = "Test" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var invalidRequest = new
        {
            amount = -50.00m, // Invalid negative amount
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Test",
            categoryId = category!.Id,
            type = "Expense"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/spending/transactions", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // DTOs for deserialization
    private record CategoryResponse(Guid Id, string Name, string? Description, Guid UserId);

    private record TransactionResponse(
        Guid Id,
        decimal Amount,
        string Currency,
        DateTime Date,
        string Description,
        Guid CategoryId,
        string Type,
        Guid UserId);
}
