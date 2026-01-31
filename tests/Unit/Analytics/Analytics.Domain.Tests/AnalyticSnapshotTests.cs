using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using FluentAssertions;

namespace Analytics.Domain.Tests;

public class AnalyticSnapshotTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var snapshotDate = new DateOnly(2025, 11, 1);
        var period = SnapshotPeriod.Monthly;
        var totalIncome = 5000m;
        var totalExpense = 3000m;
        var spendingByCategory = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 1500m },
            { Guid.NewGuid(), 1500m }
        };
        var incomeByCategory = new Dictionary<Guid, decimal>
        {
            { Guid.NewGuid(), 5000m }
        };

        // Act
        var result = AnalyticSnapshot.Create(
            userId,
            snapshotDate,
            period,
            totalIncome,
            totalExpense,
            spendingByCategory,
            incomeByCategory
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.UserId.Should().Be(userId);
        result.Value.SnapshotDate.Should().Be(snapshotDate);
        result.Value.Period.Should().Be(period);
        result.Value.TotalIncome.Should().Be(totalIncome);
        result.Value.TotalExpense.Should().Be(totalExpense);
        result.Value.NetBalance.Should().Be(2000m);
        result.Value.SpendingByCategory.Should().BeEquivalentTo(spendingByCategory);
        result.Value.IncomeByCategory.Should().BeEquivalentTo(incomeByCategory);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        // Act
        var result = AnalyticSnapshot.Create(
            Guid.Empty,
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000m,
            500m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        );

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("AnalyticSnapshot.Create");
        result.Error.Message.Should().Contain("UserId cannot be empty");
    }

    [Fact]
    public void Create_ShouldCalculateNetBalanceCorrectly()
    {
        // Arrange
        var totalIncome = 10000m;
        var totalExpense = 6500m;

        // Act
        var result = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            totalIncome,
            totalExpense,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        );

        // Assert
        result.Value.NetBalance.Should().Be(3500m);
    }

    [Theory]
    [InlineData(SnapshotPeriod.Daily)]
    [InlineData(SnapshotPeriod.Weekly)]
    [InlineData(SnapshotPeriod.Monthly)]
    [InlineData(SnapshotPeriod.Yearly)]
    public void Create_WithDifferentPeriods_ShouldWork(SnapshotPeriod period)
    {
        // Act
        var result = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            period,
            1000m,
            500m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Period.Should().Be(period);
    }

    [Fact]
    public void AddIncome_WhenCategoryDoesNotExist_ShouldAddNewCategory()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000m,
            500m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var categoryId = Guid.NewGuid();
        var amount = 500m;

        // Act
        snapshot.AddIncome(categoryId, amount);

        // Assert
        snapshot.TotalIncome.Should().Be(1500m);
        snapshot.NetBalance.Should().Be(1000m); // (1000 + 500) - 500
        snapshot.IncomeByCategory.Should().ContainKey(categoryId);
        snapshot.IncomeByCategory[categoryId].Should().Be(amount);
    }

    [Fact]
    public void AddIncome_WhenCategoryExists_ShouldUpdateExistingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000m,
            500m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal> { { categoryId, 1000m } }
        ).Value;

        var additionalAmount = 500m;

        // Act
        snapshot.AddIncome(categoryId, additionalAmount);

        // Assert
        snapshot.TotalIncome.Should().Be(1500m);
        snapshot.NetBalance.Should().Be(1000m);
        snapshot.IncomeByCategory[categoryId].Should().Be(1500m);
    }

    [Fact]
    public void AddIncome_WithMultipleCategories_ShouldTrackSeparately()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            0m,
            0m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var category1 = Guid.NewGuid();
        var category2 = Guid.NewGuid();

        // Act
        snapshot.AddIncome(category1, 1000m);
        snapshot.AddIncome(category2, 500m);
        snapshot.AddIncome(category1, 500m);

        // Assert
        snapshot.TotalIncome.Should().Be(2000m);
        snapshot.IncomeByCategory[category1].Should().Be(1500m);
        snapshot.IncomeByCategory[category2].Should().Be(500m);
    }

    [Fact]
    public void AddExpense_WhenCategoryDoesNotExist_ShouldAddNewCategory()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000m,
            500m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var categoryId = Guid.NewGuid();
        var amount = 300m;

        // Act
        snapshot.AddExpense(categoryId, amount);

        // Assert
        snapshot.TotalExpense.Should().Be(800m);
        snapshot.NetBalance.Should().Be(200m); // 1000 - (500 + 300)
        snapshot.SpendingByCategory.Should().ContainKey(categoryId);
        snapshot.SpendingByCategory[categoryId].Should().Be(amount);
    }

    [Fact]
    public void AddExpense_WhenCategoryExists_ShouldUpdateExistingCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000m,
            500m,
            new Dictionary<Guid, decimal> { { categoryId, 500m } },
            new Dictionary<Guid, decimal>()
        ).Value;

        var additionalAmount = 200m;

        // Act
        snapshot.AddExpense(categoryId, additionalAmount);

        // Assert
        snapshot.TotalExpense.Should().Be(700m);
        snapshot.NetBalance.Should().Be(300m);
        snapshot.SpendingByCategory[categoryId].Should().Be(700m);
    }

    [Fact]
    public void AddExpense_WithMultipleCategories_ShouldTrackSeparately()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            0m,
            0m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var category1 = Guid.NewGuid();
        var category2 = Guid.NewGuid();
        var category3 = Guid.NewGuid();

        // Act
        snapshot.AddExpense(category1, 300m);
        snapshot.AddExpense(category2, 150m);
        snapshot.AddExpense(category3, 100m);
        snapshot.AddExpense(category1, 200m);

        // Assert
        snapshot.TotalExpense.Should().Be(750m);
        snapshot.SpendingByCategory[category1].Should().Be(500m);
        snapshot.SpendingByCategory[category2].Should().Be(150m);
        snapshot.SpendingByCategory[category3].Should().Be(100m);
    }

    [Fact]
    public void MixedOperations_AddIncomeAndExpense_ShouldMaintainCorrectBalances()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            5000m,
            2000m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var incomeCat = Guid.NewGuid();
        var expenseCat = Guid.NewGuid();

        // Act
        snapshot.AddIncome(incomeCat, 1000m);
        snapshot.AddExpense(expenseCat, 500m);
        snapshot.AddIncome(incomeCat, 500m);
        snapshot.AddExpense(expenseCat, 300m);

        // Assert
        snapshot.TotalIncome.Should().Be(6500m);
        snapshot.TotalExpense.Should().Be(2800m);
        snapshot.NetBalance.Should().Be(3700m);
        snapshot.IncomeByCategory[incomeCat].Should().Be(1500m);
        snapshot.SpendingByCategory[expenseCat].Should().Be(800m);
    }

    [Fact]
    public void NetBalance_WhenExpenseExceedsIncome_ShouldBeNegative()
    {
        // Arrange & Act
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000m,
            1500m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        // Assert
        snapshot.NetBalance.Should().Be(-500m);
    }

    [Fact]
    public void Create_WithEmptyDictionaries_ShouldWork()
    {
        // Act
        var result = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            0m,
            0m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SpendingByCategory.Should().BeEmpty();
        result.Value.IncomeByCategory.Should().BeEmpty();
        result.Value.TotalIncome.Should().Be(0m);
        result.Value.TotalExpense.Should().Be(0m);
        result.Value.NetBalance.Should().Be(0m);
    }

    [Fact]
    public void AddIncome_WithDecimalPrecision_ShouldMaintainAccuracy()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            100.50m,
            50.25m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var categoryId = Guid.NewGuid();

        // Act
        snapshot.AddIncome(categoryId, 123.45m);

        // Assert
        snapshot.TotalIncome.Should().Be(223.95m);
        snapshot.NetBalance.Should().Be(173.70m);
    }

    [Fact]
    public void AddExpense_WithDecimalPrecision_ShouldMaintainAccuracy()
    {
        // Arrange
        var snapshot = AnalyticSnapshot.Create(
            Guid.NewGuid(),
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            1000.00m,
            100.00m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var categoryId = Guid.NewGuid();

        // Act
        snapshot.AddExpense(categoryId, 99.99m);

        // Assert
        snapshot.TotalExpense.Should().Be(199.99m);
        snapshot.NetBalance.Should().Be(800.01m);
    }
}
