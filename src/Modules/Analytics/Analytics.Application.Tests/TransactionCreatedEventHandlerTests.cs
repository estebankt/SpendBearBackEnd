using Analytics.Application.Features.EventHandlers;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using FluentAssertions;
using Moq;
using Spending.Domain.Entities;
using Spending.Domain.Events;
using SpendBear.SharedKernel;

namespace Analytics.Application.Tests;

public class TransactionCreatedEventHandlerTests
{
    private readonly Mock<IAnalyticSnapshotRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly TransactionCreatedEventHandler _handler;

    public TransactionCreatedEventHandlerTests()
    {
        _mockRepository = new Mock<IAnalyticSnapshotRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new TransactionCreatedEventHandler(
            _mockRepository.Object,
            _mockUnitOfWork.Object
        );
    }

    [Fact]
    public async Task Handle_WhenSnapshotDoesNotExist_AndTransactionIsExpense_ShouldCreateNewSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 11, 15);
        var amount = 150.50m;

        var @event = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            amount,
            "USD",
            TransactionType.Expense,
            categoryId,
            transactionDate
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<SnapshotPeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticSnapshot?)null);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.AddAsync(
                It.Is<AnalyticSnapshot>(s =>
                    s.UserId == userId &&
                    s.SnapshotDate == new DateOnly(2025, 11, 1) &&
                    s.Period == SnapshotPeriod.Monthly &&
                    s.TotalExpense == amount &&
                    s.TotalIncome == 0 &&
                    s.SpendingByCategory.ContainsKey(categoryId) &&
                    s.SpendingByCategory[categoryId] == amount
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSnapshotDoesNotExist_AndTransactionIsIncome_ShouldCreateNewSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 11, 20);
        var amount = 2500.00m;

        var @event = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            amount,
            "USD",
            TransactionType.Income,
            categoryId,
            transactionDate
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<SnapshotPeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticSnapshot?)null);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.AddAsync(
                It.Is<AnalyticSnapshot>(s =>
                    s.UserId == userId &&
                    s.TotalIncome == amount &&
                    s.TotalExpense == 0 &&
                    s.IncomeByCategory.ContainsKey(categoryId) &&
                    s.IncomeByCategory[categoryId] == amount
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenSnapshotExists_AndTransactionIsExpense_ShouldUpdateSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var existingSnapshot = AnalyticSnapshot.Create(
            userId,
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            5000m,
            2000m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var transactionDate = new DateTime(2025, 11, 15);
        var amount = 150.50m;

        var @event = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            amount,
            "USD",
            TransactionType.Expense,
            categoryId,
            transactionDate
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                userId,
                new DateOnly(2025, 11, 1),
                SnapshotPeriod.Monthly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSnapshot);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        existingSnapshot.TotalExpense.Should().Be(2150.50m);
        existingSnapshot.SpendingByCategory[categoryId].Should().Be(150.50m);

        _mockRepository.Verify(
            x => x.UpdateAsync(existingSnapshot, It.IsAny<CancellationToken>()),
            Times.Once
        );

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSnapshotExists_AndTransactionIsIncome_ShouldUpdateSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var existingSnapshot = AnalyticSnapshot.Create(
            userId,
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            5000m,
            2000m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        var transactionDate = new DateTime(2025, 11, 10);
        var amount = 1000m;

        var @event = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            amount,
            "USD",
            TransactionType.Income,
            categoryId,
            transactionDate
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                userId,
                new DateOnly(2025, 11, 1),
                SnapshotPeriod.Monthly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSnapshot);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        existingSnapshot.TotalIncome.Should().Be(6000m);
        existingSnapshot.IncomeByCategory[categoryId].Should().Be(1000m);

        _mockRepository.Verify(
            x => x.UpdateAsync(existingSnapshot, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldUseFirstDayOfMonth_ForMonthlySnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 11, 25); // 25th of the month

        var @event = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            100m,
            "USD",
            TransactionType.Expense,
            categoryId,
            transactionDate
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<SnapshotPeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticSnapshot?)null);

        AnalyticSnapshot? capturedSnapshot = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<AnalyticSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AnalyticSnapshot, CancellationToken>((s, ct) => capturedSnapshot = s);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedSnapshot.Should().NotBeNull();
        capturedSnapshot!.SnapshotDate.Should().Be(new DateOnly(2025, 11, 1));
    }

    [Fact]
    public async Task Handle_WithMultipleTransactionsSameMonth_ShouldAccumulateInSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category1 = Guid.NewGuid();
        var category2 = Guid.NewGuid();

        var snapshot = AnalyticSnapshot.Create(
            userId,
            new DateOnly(2025, 11, 1),
            SnapshotPeriod.Monthly,
            0m,
            0m,
            new Dictionary<Guid, decimal>(),
            new Dictionary<Guid, decimal>()
        ).Value;

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                userId,
                new DateOnly(2025, 11, 1),
                SnapshotPeriod.Monthly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        var event1 = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            100m,
            "USD",
            TransactionType.Expense,
            category1,
            new DateTime(2025, 11, 5)
        );

        var event2 = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            200m,
            "USD",
            TransactionType.Expense,
            category1,
            new DateTime(2025, 11, 10)
        );

        var event3 = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            50m,
            "USD",
            TransactionType.Expense,
            category2,
            new DateTime(2025, 11, 15)
        );

        // Act
        await _handler.Handle(event1, CancellationToken.None);
        await _handler.Handle(event2, CancellationToken.None);
        await _handler.Handle(event3, CancellationToken.None);

        // Assert
        snapshot.TotalExpense.Should().Be(350m);
        snapshot.SpendingByCategory[category1].Should().Be(300m);
        snapshot.SpendingByCategory[category2].Should().Be(50m);

        _mockRepository.Verify(
            x => x.UpdateAsync(snapshot, It.IsAny<CancellationToken>()),
            Times.Exactly(3)
        );
    }

    [Fact]
    public async Task Handle_WithDecimalPrecision_ShouldMaintainAccuracy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var transactionDate = new DateTime(2025, 11, 15);
        var amount = 123.45m;

        var @event = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            amount,
            "USD",
            TransactionType.Expense,
            categoryId,
            transactionDate
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<SnapshotPeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticSnapshot?)null);

        AnalyticSnapshot? capturedSnapshot = null;
        _mockRepository
            .Setup(x => x.AddAsync(It.IsAny<AnalyticSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<AnalyticSnapshot, CancellationToken>((s, ct) => capturedSnapshot = s);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        capturedSnapshot.Should().NotBeNull();
        capturedSnapshot!.TotalExpense.Should().Be(123.45m);
        capturedSnapshot.SpendingByCategory[categoryId].Should().Be(123.45m);
    }

    [Fact]
    public async Task Handle_WithDifferentMonths_ShouldCreateSeparateSnapshots()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var eventNovember = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            100m,
            "USD",
            TransactionType.Expense,
            categoryId,
            new DateTime(2025, 11, 15)
        );

        var eventDecember = new TransactionCreatedEvent(
            Guid.NewGuid(),
            userId,
            200m,
            "USD",
            TransactionType.Expense,
            categoryId,
            new DateTime(2025, 12, 15)
        );

        _mockRepository
            .Setup(x => x.GetByUserIdAndDateAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<SnapshotPeriod>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AnalyticSnapshot?)null);

        // Act
        await _handler.Handle(eventNovember, CancellationToken.None);
        await _handler.Handle(eventDecember, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.AddAsync(
                It.Is<AnalyticSnapshot>(s => s.SnapshotDate == new DateOnly(2025, 11, 1)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _mockRepository.Verify(
            x => x.AddAsync(
                It.Is<AnalyticSnapshot>(s => s.SnapshotDate == new DateOnly(2025, 12, 1)),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }
}
