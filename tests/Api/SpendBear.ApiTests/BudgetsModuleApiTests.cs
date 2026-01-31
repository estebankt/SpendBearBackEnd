using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SpendBear.ApiTests;

/// <summary>
/// API tests for Budgets Module endpoints.
/// Tests budget CRUD operations and event-driven integration with transactions.
/// </summary>
[Collection("API Tests")]
public class BudgetsModuleApiTests : ApiTestBase
{
    [Fact]
    public async Task CreateBudget_WithValidData_ReturnsCreated()
    {
        // Arrange
        var categoryRequest = new { name = "Groceries", description = "Food shopping" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var budgetRequest = new
        {
            name = "Monthly Grocery Budget",
            amount = 500.00m,
            currency = "USD",
            period = "Monthly",
            categoryId = category!.Id,
            warningThreshold = 80.0m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/budgets", budgetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var budget = await response.Content.ReadFromJsonAsync<BudgetResponse>();
        budget.Should().NotBeNull();
        budget!.Id.Should().NotBeEmpty();
        budget.Name.Should().Be("Monthly Grocery Budget");
        budget.Amount.Should().Be(500.00m);
        budget.Period.Should().Be("Monthly");
    }

    [Fact]
    public async Task GetBudgets_AfterCreatingBudget_ReturnsBudgets()
    {
        // Arrange - Create category and budget
        var categoryRequest = new { name = "Utilities", description = "Bills" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var budgetRequest = new
        {
            name = "Monthly Utilities Budget",
            amount = 300.00m,
            currency = "USD",
            period = "Monthly",
            categoryId = category!.Id,
            warningThreshold = 75.0m
        };
        await Client.PostAsJsonAsync("/api/budgets", budgetRequest);

        // Act
        var response = await Client.GetAsync("/api/budgets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetResponse>>();
        budgets.Should().NotBeNull();
        budgets.Should().ContainSingle(b => b.Name == "Monthly Utilities Budget");
    }

    [Fact]
    public async Task UpdateBudget_WithValidData_ReturnsOk()
    {
        // Arrange - Create category and budget
        var categoryRequest = new { name = "Entertainment", description = "Fun stuff" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var createRequest = new
        {
            name = "Monthly Entertainment Budget",
            amount = 200.00m,
            currency = "USD",
            period = "Monthly",
            categoryId = category!.Id,
            warningThreshold = 80.0m
        };
        var createResponse = await Client.PostAsJsonAsync("/api/budgets", createRequest);
        var budget = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>();

        var updateRequest = new
        {
            name = "Monthly Entertainment Budget (Updated)",
            amount = 250.00m,
            currency = "USD",
            period = "Monthly",
            categoryId = category.Id,
            warningThreshold = 75.0m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/budgets/{budget!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<BudgetResponse>();
        updated.Should().NotBeNull();
        updated!.Amount.Should().Be(250.00m);
        updated.Name.Should().Contain("Updated");
    }

    [Fact]
    public async Task DeleteBudget_WithValidId_ReturnsNoContent()
    {
        // Arrange - Create category and budget
        var categoryRequest = new { name = "Transportation", description = "Travel" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var createRequest = new
        {
            name = "Monthly Transport Budget",
            amount = 150.00m,
            currency = "USD",
            period = "Monthly",
            categoryId = category!.Id,
            warningThreshold = 80.0m
        };
        var createResponse = await Client.PostAsJsonAsync("/api/budgets", createRequest);
        var budget = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/budgets/{budget!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await Client.GetAsync("/api/budgets");
        var budgets = await getResponse.Content.ReadFromJsonAsync<List<BudgetResponse>>();
        budgets.Should().NotContain(b => b.Id == budget.Id);
    }

    [Fact]
    public async Task CreateBudget_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var categoryRequest = new { name = "Test", description = "Test" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var invalidRequest = new
        {
            name = "Invalid Budget",
            amount = -100.00m, // Invalid negative amount
            currency = "USD",
            period = "Monthly",
            categoryId = category!.Id,
            warningThreshold = 80.0m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/budgets", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGlobalBudget_WithoutCategoryId_ReturnsCreated()
    {
        // Arrange - Create a global budget (no category)
        var budgetRequest = new
        {
            name = "Total Monthly Budget",
            amount = 2000.00m,
            currency = "USD",
            period = "Monthly",
            categoryId = (Guid?)null, // Global budget
            warningThreshold = 85.0m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/budgets", budgetRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var budget = await response.Content.ReadFromJsonAsync<BudgetResponse>();
        budget.Should().NotBeNull();
        budget!.Name.Should().Be("Total Monthly Budget");
        budget.CategoryId.Should().BeNull();
    }

    // DTOs for deserialization
    private record CategoryResponse(Guid Id, string Name, string? Description, Guid UserId);

    private record BudgetResponse(
        Guid Id,
        string Name,
        decimal Amount,
        string Currency,
        string Period,
        Guid? CategoryId,
        decimal WarningThreshold,
        Guid UserId);
}
