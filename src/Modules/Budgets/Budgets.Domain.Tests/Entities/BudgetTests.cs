using Budgets.Domain.Entities;
using Budgets.Domain.Enums;
using Budgets.Domain.Events;
using FluentAssertions;

namespace Budgets.Domain.Tests.Entities;

public class BudgetTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var name = "Monthly Budget";
        var amount = 1000m;
        var currency = "USD";
        var period = BudgetPeriod.Monthly;
        var startDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var warningThreshold = 80m;

        // Act
        var result = Budget.Create(name, amount, currency, period, startDate, userId, categoryId, warningThreshold);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Amount.Should().Be(amount);
        result.Value.Currency.Should().Be(currency);
        result.Value.Period.Should().Be(period);
        result.Value.StartDate.Should().Be(startDate);
        result.Value.UserId.Should().Be(userId);
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.WarningThreshold.Should().Be(warningThreshold);
        result.Value.CurrentSpent.Should().Be(0);
        result.Value.IsExceeded.Should().BeFalse();
        result.Value.WarningTriggered.Should().BeFalse();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Arrange
        var amount = 1000m;
        var currency = "USD";
        var period = BudgetPeriod.Monthly;
        var startDate = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var result = Budget.Create("", amount, currency, period, startDate, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidName");
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldFail()
    {
        // Arrange
        var name = "Budget";
        var currency = "USD";
        var period = BudgetPeriod.Monthly;
        var startDate = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var result = Budget.Create(name, 0, currency, period, startDate, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidAmount");
    }

    [Fact]
    public void Create_WithInvalidCurrency_ShouldFail()
    {
        // Arrange
        var name = "Budget";
        var amount = 1000m;
        var period = BudgetPeriod.Monthly;
        var startDate = DateTime.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var result = Budget.Create(name, amount, "US", period, startDate, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidCurrency");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        // Arrange
        var name = "Budget";
        var amount = 1000m;
        var currency = "USD";
        var period = BudgetPeriod.Monthly;
        var startDate = DateTime.UtcNow;

        // Act
        var result = Budget.Create(name, amount, currency, period, startDate, Guid.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidUser");
    }

    [Fact]
    public void Create_ShouldRaiseBudgetCreatedEvent()
    {
        // Arrange
        var name = "Budget";
        var amount = 1000m;
        var currency = "USD";
        var period = BudgetPeriod.Monthly;
        var startDate = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        // Act
        var result = Budget.Create(name, amount, currency, period, startDate, userId, categoryId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().HaveCount(1);
        result.Value.DomainEvents.First().Should().BeOfType<BudgetCreatedEvent>();

        var domainEvent = result.Value.DomainEvents.First() as BudgetCreatedEvent;
        domainEvent!.BudgetId.Should().Be(result.Value.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Name.Should().Be(name);
        domainEvent.Amount.Should().Be(amount);
        domainEvent.Currency.Should().Be(currency);
        domainEvent.CategoryId.Should().Be(categoryId);
    }

    [Theory]
    [InlineData(BudgetPeriod.Daily, 1)]
    [InlineData(BudgetPeriod.Weekly, 7)]
    [InlineData(BudgetPeriod.Monthly, 31)] // December has 31 days
    [InlineData(BudgetPeriod.Yearly, 365)]
    public void Create_ShouldCalculateCorrectEndDate(BudgetPeriod period, int expectedDays)
    {
        // Arrange
        var startDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();

        // Act
        var result = Budget.Create("Budget", 1000m, "USD", period, startDate, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var expectedEndDate = period switch
        {
            BudgetPeriod.Daily => startDate.AddDays(1).AddSeconds(-1),
            BudgetPeriod.Weekly => startDate.AddDays(7).AddSeconds(-1),
            BudgetPeriod.Monthly => startDate.AddMonths(1).AddSeconds(-1),
            BudgetPeriod.Yearly => startDate.AddYears(1).AddSeconds(-1),
            _ => throw new ArgumentException()
        };
        result.Value.EndDate.Should().Be(expectedEndDate);
    }

    [Fact]
    public void RecordTransaction_ShouldUpdateCurrentSpent()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;
        budget.ClearDomainEvents();

        // Act
        budget.RecordTransaction(150m);

        // Assert
        budget.CurrentSpent.Should().Be(150m);
        budget.RemainingAmount.Should().Be(850m);
        budget.PercentageUsed.Should().BeApproximately(15m, 0.01m);
    }

    [Fact]
    public void RecordTransaction_WhenReachingThreshold_ShouldRaiseWarningEvent()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid(), null, 80m).Value;
        budget.ClearDomainEvents();

        // Act
        budget.RecordTransaction(800m); // 80% of budget

        // Assert
        budget.WarningTriggered.Should().BeTrue();
        budget.DomainEvents.Should().Contain(e => e is BudgetWarningEvent);

        var warningEvent = budget.DomainEvents.OfType<BudgetWarningEvent>().First();
        warningEvent.BudgetId.Should().Be(budget.Id);
        warningEvent.PercentageUsed.Should().BeApproximately(80m, 0.01m);
        warningEvent.ThresholdPercentage.Should().Be(80m);
    }

    [Fact]
    public void RecordTransaction_WhenExceedingBudget_ShouldRaiseExceededEvent()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;
        budget.ClearDomainEvents();

        // Act
        budget.RecordTransaction(1100m); // Exceeds budget by 100

        // Assert
        budget.IsExceeded.Should().BeTrue();
        budget.CurrentSpent.Should().Be(1100m);
        budget.RemainingAmount.Should().Be(-100m);
        budget.DomainEvents.Should().Contain(e => e is BudgetExceededEvent);

        var exceededEvent = budget.DomainEvents.OfType<BudgetExceededEvent>().First();
        exceededEvent.BudgetId.Should().Be(budget.Id);
        exceededEvent.CurrentSpent.Should().Be(1100m);
        exceededEvent.ExceededBy.Should().Be(100m);
    }

    [Fact]
    public void RecordTransaction_MultipleTransactions_ShouldAccumulate()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;

        // Act
        budget.RecordTransaction(100m);
        budget.RecordTransaction(200m);
        budget.RecordTransaction(300m);

        // Assert
        budget.CurrentSpent.Should().Be(600m);
        budget.PercentageUsed.Should().BeApproximately(60m, 0.01m);
    }

    [Fact]
    public void Update_ShouldUpdateProperties()
    {
        // Arrange
        var budget = Budget.Create("Old Name", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;
        budget.ClearDomainEvents();

        var newName = "New Name";
        var newAmount = 1500m;
        var newPeriod = BudgetPeriod.Weekly;
        var newStartDate = DateTime.UtcNow.AddDays(1);
        var newCategoryId = Guid.NewGuid();
        var newThreshold = 75m;

        // Act
        budget.Update(newName, newAmount, newPeriod, newStartDate, newCategoryId, newThreshold);

        // Assert
        budget.Name.Should().Be(newName);
        budget.Amount.Should().Be(newAmount);
        budget.Period.Should().Be(newPeriod);
        budget.StartDate.Should().Be(newStartDate);
        budget.CategoryId.Should().Be(newCategoryId);
        budget.WarningThreshold.Should().Be(newThreshold);
        budget.DomainEvents.Should().Contain(e => e is BudgetUpdatedEvent);
    }

    [Fact]
    public void Update_WhenIncreasingAmount_ShouldClearExceededFlag()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;
        budget.RecordTransaction(1100m); // Exceeds budget
        budget.ClearDomainEvents();

        // Act
        budget.Update("Budget", 1500m, BudgetPeriod.Monthly, DateTime.UtcNow, null, 80m);

        // Assert
        budget.IsExceeded.Should().BeFalse(); // Now within budget
        budget.CurrentSpent.Should().Be(1100m);
        budget.Amount.Should().Be(1500m);
    }

    [Fact]
    public void ResetForNewPeriod_ShouldResetSpendingAndFlags()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;
        budget.RecordTransaction(1100m); // Exceeds budget
        var newStartDate = DateTime.UtcNow.AddMonths(1);

        // Act
        budget.ResetForNewPeriod(newStartDate);

        // Assert
        budget.CurrentSpent.Should().Be(0);
        budget.IsExceeded.Should().BeFalse();
        budget.WarningTriggered.Should().BeFalse();
        budget.StartDate.Should().Be(newStartDate);
    }

    [Fact]
    public void IsInPeriod_WithDateInRange_ShouldReturnTrue()
    {
        // Arrange
        var startDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, startDate, Guid.NewGuid()).Value;
        var testDate = new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = budget.IsInPeriod(testDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsInPeriod_WithDateOutsideRange_ShouldReturnFalse()
    {
        // Arrange
        var startDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, startDate, Guid.NewGuid()).Value;
        var testDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = budget.IsInPeriod(testDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var budget = Budget.Create("Budget", 1000m, "USD", BudgetPeriod.Monthly, DateTime.UtcNow, Guid.NewGuid()).Value;
        budget.RecordTransaction(250m);

        // Act & Assert
        budget.RemainingAmount.Should().Be(750m);
        budget.PercentageUsed.Should().BeApproximately(25m, 0.01m);
    }
}
