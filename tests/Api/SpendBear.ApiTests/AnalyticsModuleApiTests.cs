using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SpendBear.ApiTests;

/// <summary>
/// API tests for Analytics Module endpoints.
/// Tests monthly summary queries and event-driven snapshot creation.
/// </summary>
[Collection("API Tests")]
public class AnalyticsModuleApiTests : ApiTestBase
{
    [Fact]
    public async Task GetMonthlySummary_WithNoData_ReturnsEmptyOrNotFound()
    {
        // Arrange
        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        // Act
        var response = await Client.GetAsync($"/api/analytics/summary/monthly?year={year}&month={month}");

        // Assert
        // Either empty data or 404 is acceptable for no data
        (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound)
            .Should().BeTrue();
    }

    [Fact]
    public async Task GetMonthlySummary_AfterCreatingTransaction_ReturnsSnapshot()
    {
        // Arrange - Create category and transaction
        var categoryRequest = new { name = "Food", description = "Food expenses" };
        var categoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", categoryRequest);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var transactionRequest = new
        {
            amount = 75.50m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Dinner",
            categoryId = category!.Id,
            type = "Expense"
        };
        await Client.PostAsJsonAsync("/api/spending/transactions", transactionRequest);

        // Wait for async event processing
        await Task.Delay(500);

        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        // Act
        var response = await Client.GetAsync($"/api/analytics/summary/monthly?year={year}&month={month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<MonthlySummaryResponse>();
        summary.Should().NotBeNull();
        summary!.TotalExpense.Should().BeGreaterThan(0);
        summary.Year.Should().Be(year);
        summary.Month.Should().Be(month);
    }

    [Fact]
    public async Task GetMonthlySummary_WithIncomeAndExpense_CalculatesNetBalance()
    {
        // Arrange - Create categories
        var incomeCategoryRequest = new { name = "Salary", description = "Income" };
        var incomeCategoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", incomeCategoryRequest);
        var incomeCategory = await incomeCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var expenseCategoryRequest = new { name = "Bills", description = "Expenses" };
        var expenseCategoryResponse = await Client.PostAsJsonAsync("/api/spending/categories", expenseCategoryRequest);
        var expenseCategory = await expenseCategoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Create income transaction
        var incomeRequest = new
        {
            amount = 1000.00m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Salary",
            categoryId = incomeCategory!.Id,
            type = "Income"
        };
        await Client.PostAsJsonAsync("/api/spending/transactions", incomeRequest);

        // Create expense transaction
        var expenseRequest = new
        {
            amount = 300.00m,
            currency = "USD",
            date = DateTime.UtcNow,
            description = "Rent",
            categoryId = expenseCategory!.Id,
            type = "Expense"
        };
        await Client.PostAsJsonAsync("/api/spending/transactions", expenseRequest);

        // Wait for async event processing
        await Task.Delay(500);

        var year = DateTime.UtcNow.Year;
        var month = DateTime.UtcNow.Month;

        // Act
        var response = await Client.GetAsync($"/api/analytics/summary/monthly?year={year}&month={month}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<MonthlySummaryResponse>();
        summary.Should().NotBeNull();
        summary!.TotalIncome.Should().BeGreaterThan(0);
        summary.TotalExpense.Should().BeGreaterThan(0);
        summary.NetBalance.Should().Be(summary.TotalIncome - summary.TotalExpense);
    }

    [Fact]
    public async Task GetMonthlySummary_WithInvalidDate_ReturnsBadRequest()
    {
        // Arrange
        var invalidYear = 0;
        var invalidMonth = 13;

        // Act
        var response = await Client.GetAsync($"/api/analytics/summary/monthly?year={invalidYear}&month={invalidMonth}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // DTOs for deserialization
    private record CategoryResponse(Guid Id, string Name, string? Description, Guid UserId);

    private record MonthlySummaryResponse(
        Guid Id,
        int Year,
        int Month,
        decimal TotalIncome,
        decimal TotalExpense,
        decimal NetBalance,
        string Period);
}
