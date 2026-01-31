using Budgets.Application.Features.EventHandlers;
using Budgets.Domain.Entities;
using Budgets.Domain.Enums;
using Budgets.Domain.Repositories;
using FluentAssertions;
using Moq;
using SpendBear.SharedKernel;

namespace Budgets.Application.Tests.Features;

public class TransactionCreatedEventHandlerTests
{
    private readonly Mock<IBudgetRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly TransactionCreatedEventHandler _handler;

    public TransactionCreatedEventHandlerTests()
    {
        _mockRepository = new Mock<IBudgetRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new TransactionCreatedEventHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithExpenseTransaction_ShouldUpdateMatchingBudgets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;
        var transactionAmount = 50m;

        var budget = Budget.Create(
            "Groceries Budget",
            500m,
            "USD",
            BudgetPeriod.Monthly,
            transactionDate.AddDays(-10),
            userId,
            categoryId,
            80m
        ).Value;

        _mockRepository.Setup(r => r.GetActiveBudgetsForUserAsync(userId, transactionDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { budget });

        // Act
        await _handler.Handle(
            Guid.NewGuid(), // transactionId
            userId,
            transactionAmount,
            "USD",
            0, // Expense
            categoryId,
            transactionDate
        );

        // Assert
        budget.CurrentSpent.Should().Be(transactionAmount);
        _mockRepository.Verify(r => r.UpdateAsync(budget, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithIncomeTransaction_ShouldNotUpdateBudgets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        // Act
        await _handler.Handle(
            Guid.NewGuid(),
            userId,
            100m,
            "USD",
            1, // Income
            Guid.NewGuid(),
            transactionDate
        );

        // Assert
        _mockRepository.Verify(r => r.GetActiveBudgetsForUserAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMismatchedCurrency_ShouldNotUpdateBudget()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        var budget = Budget.Create(
            "USD Budget",
            500m,
            "USD",
            BudgetPeriod.Monthly,
            transactionDate.AddDays(-10),
            userId,
            categoryId,
            80m
        ).Value;

        _mockRepository.Setup(r => r.GetActiveBudgetsForUserAsync(userId, transactionDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { budget });

        // Act
        await _handler.Handle(
            Guid.NewGuid(),
            userId,
            50m,
            "EUR", // Different currency
            0,
            categoryId,
            transactionDate
        );

        // Assert
        budget.CurrentSpent.Should().Be(0); // Should not be updated
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithGlobalBudget_ShouldUpdateRegardlessOfCategory()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;
        var transactionAmount = 75m;

        var globalBudget = Budget.Create(
            "Monthly Spending",
            2000m,
            "USD",
            BudgetPeriod.Monthly,
            transactionDate.AddDays(-10),
            userId,
            null, // Global budget (no category)
            80m
        ).Value;

        _mockRepository.Setup(r => r.GetActiveBudgetsForUserAsync(userId, transactionDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { globalBudget });

        // Act
        await _handler.Handle(
            Guid.NewGuid(),
            userId,
            transactionAmount,
            "USD",
            0,
            Guid.NewGuid(), // Any category
            transactionDate
        );

        // Assert
        globalBudget.CurrentSpent.Should().Be(transactionAmount);
        _mockRepository.Verify(r => r.UpdateAsync(globalBudget, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleBudgets_ShouldUpdateAllMatching()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;
        var transactionAmount = 100m;

        var categoryBudget = Budget.Create("Category Budget", 500m, "USD", BudgetPeriod.Monthly, transactionDate.AddDays(-10), userId, categoryId, 80m).Value;
        var globalBudget = Budget.Create("Global Budget", 2000m, "USD", BudgetPeriod.Monthly, transactionDate.AddDays(-10), userId, null, 80m).Value;

        _mockRepository.Setup(r => r.GetActiveBudgetsForUserAsync(userId, transactionDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { categoryBudget, globalBudget });

        // Act
        await _handler.Handle(
            Guid.NewGuid(),
            userId,
            transactionAmount,
            "USD",
            0,
            categoryId,
            transactionDate
        );

        // Assert
        categoryBudget.CurrentSpent.Should().Be(transactionAmount);
        globalBudget.CurrentSpent.Should().Be(transactionAmount);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenCategoryDoesNotMatch_ShouldNotUpdateCategoryBudget()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var groceryCategoryId = Guid.NewGuid();
        var entertainmentCategoryId = Guid.NewGuid();
        var transactionDate = DateTime.UtcNow;

        var groceryBudget = Budget.Create("Grocery Budget", 500m, "USD", BudgetPeriod.Monthly, transactionDate.AddDays(-10), userId, groceryCategoryId, 80m).Value;

        _mockRepository.Setup(r => r.GetActiveBudgetsForUserAsync(userId, transactionDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Budget> { groceryBudget });

        // Act
        await _handler.Handle(
            Guid.NewGuid(),
            userId,
            50m,
            "USD",
            0,
            entertainmentCategoryId, // Different category
            transactionDate
        );

        // Assert
        groceryBudget.CurrentSpent.Should().Be(0); // Should not be updated
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
