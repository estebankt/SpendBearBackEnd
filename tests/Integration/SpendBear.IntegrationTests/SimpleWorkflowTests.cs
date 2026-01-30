using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Spending.Domain.Repositories;
using Spending.Domain.ValueObjects;
using Xunit;

namespace SpendBear.IntegrationTests;

/// <summary>
/// Simple end-to-end test that verifies the Transaction → Analytics workflow
/// </summary>
public class SimpleWorkflowTests : IntegrationTestBase
{
    [Fact]
    public async Task Canary_Test_ShouldPass()
    {
        // This test just verifies the infrastructure is working
        using var scope = Services.CreateScope();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();

        categoryRepo.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCategory_AndTransaction_ShouldCreateAnalyticsSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryName = "Test Category";
        var transactionAmount = 100.50m;
        var transactionDate = DateTime.UtcNow;

        using var scope = Services.CreateScope();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var transactionRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var analyticsRepo = scope.ServiceProvider.GetRequiredService<IAnalyticSnapshotRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<SpendBear.SharedKernel.IUnitOfWork>();

        // Act - Create category
        var categoryResult = Spending.Domain.Entities.Category.Create(categoryName, "Test Description", userId);
        categoryResult.IsSuccess.Should().BeTrue();
        var category = categoryResult.Value;

        await categoryRepo.AddAsync(category);
        await unitOfWork.SaveChangesAsync();

        // Act - Create transaction
        var money = Money.Create(transactionAmount, "USD").Value;
        var transactionResult = Spending.Domain.Entities.Transaction.Create(
            money,
            transactionDate,
            "Test Transaction",
            category.Id,
            userId,
            Spending.Domain.Entities.TransactionType.Expense
        );

        transactionResult.IsSuccess.Should().BeTrue();
        var transaction = transactionResult.Value;

        await transactionRepo.AddAsync(transaction);
        await unitOfWork.SaveChangesAsync();

        // Give time for event processing
        await Task.Delay(200);

        // Assert - Analytics snapshot should be created
        var firstDayOfMonth = new DateOnly(transactionDate.Year, transactionDate.Month, 1);
        var snapshot = await analyticsRepo.GetByUserIdAndDateAsync(userId, firstDayOfMonth, SnapshotPeriod.Monthly);

        snapshot.Should().NotBeNull("Analytics snapshot should be created after transaction");
        snapshot!.TotalExpense.Should().Be(transactionAmount);
        snapshot.TotalIncome.Should().Be(0);
        snapshot.NetBalance.Should().Be(-transactionAmount);
    }

    [Fact]
    public async Task CreateMultipleTransactions_ShouldAggregateInAnalytics()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        using var scope = Services.CreateScope();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ICategoryRepository>();
        var transactionRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var analyticsRepo = scope.ServiceProvider.GetRequiredService<IAnalyticSnapshotRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<SpendBear.SharedKernel.IUnitOfWork>();

        // Create category
        var category = Spending.Domain.Entities.Category.Create("Food", "Food expenses", userId).Value;
        await categoryRepo.AddAsync(category);
        await unitOfWork.SaveChangesAsync();

        // Act - Create 3 transactions
        var amounts = new[] { 50m, 75m, 100m };
        foreach (var amount in amounts)
        {
            var money = Money.Create(amount, "USD").Value;
            var transaction = Spending.Domain.Entities.Transaction.Create(
                money,
                transactionDate,
                $"Transaction {amount}",
                category.Id,
                userId,
                Spending.Domain.Entities.TransactionType.Expense
            ).Value;

            await transactionRepo.AddAsync(transaction);
            await unitOfWork.SaveChangesAsync();
            await Task.Delay(100); // Give time for each event to process
        }

        // Assert
        var firstDayOfMonth = new DateOnly(transactionDate.Year, transactionDate.Month, 1);
        var snapshot = await analyticsRepo.GetByUserIdAndDateAsync(userId, firstDayOfMonth, SnapshotPeriod.Monthly);

        snapshot.Should().NotBeNull();
        snapshot!.TotalExpense.Should().Be(225m); // 50 + 75 + 100
    }
}
