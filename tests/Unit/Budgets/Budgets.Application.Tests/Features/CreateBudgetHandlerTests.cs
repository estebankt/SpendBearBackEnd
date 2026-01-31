using Budgets.Application.Features.Budgets.CreateBudget;
using Budgets.Domain.Entities;
using Budgets.Domain.Enums;
using Budgets.Domain.Repositories;
using FluentAssertions;
using Moq;
using SpendBear.SharedKernel;

namespace Budgets.Application.Tests.Features;

public class CreateBudgetHandlerTests
{
    private readonly Mock<IBudgetRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateBudgetHandler _handler;

    public CreateBudgetHandlerTests()
    {
        _mockRepository = new Mock<IBudgetRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new CreateBudgetHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            Name: "Monthly Budget",
            Amount: 1000m,
            Currency: "USD",
            Period: BudgetPeriod.Monthly,
            StartDate: DateTime.UtcNow,
            CategoryId: Guid.NewGuid(),
            WarningThreshold: 80m
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Amount.Should().Be(command.Amount);
        result.Value.Currency.Should().Be(command.Currency);
        result.Value.Period.Should().Be(command.Period);
        result.Value.CategoryId.Should().Be(command.CategoryId);
        result.Value.WarningThreshold.Should().Be(command.WarningThreshold);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidName_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            Name: "",
            Amount: 1000m,
            Currency: "USD",
            Period: BudgetPeriod.Monthly,
            StartDate: DateTime.UtcNow,
            CategoryId: null,
            WarningThreshold: 80m
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidName");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAmount_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            Name: "Budget",
            Amount: 0m,
            Currency: "USD",
            Period: BudgetPeriod.Monthly,
            StartDate: DateTime.UtcNow,
            CategoryId: null,
            WarningThreshold: 80m
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidAmount");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidCurrency_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            Name: "Budget",
            Amount: 1000m,
            Currency: "US", // Invalid - should be 3 characters
            Period: BudgetPeriod.Monthly,
            StartDate: DateTime.UtcNow,
            CategoryId: null,
            WarningThreshold: 80m
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Budget.InvalidCurrency");
    }

    [Fact]
    public async Task Handle_WithNullCategoryId_ShouldCreateGlobalBudget()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            Name: "Global Budget",
            Amount: 2000m,
            Currency: "USD",
            Period: BudgetPeriod.Monthly,
            StartDate: DateTime.UtcNow,
            CategoryId: null, // Global budget
            WarningThreshold: 85m
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().BeNull();
        result.Value.Name.Should().Be(command.Name);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Budget>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(BudgetPeriod.Daily)]
    [InlineData(BudgetPeriod.Weekly)]
    [InlineData(BudgetPeriod.Monthly)]
    [InlineData(BudgetPeriod.Yearly)]
    public async Task Handle_WithDifferentPeriods_ShouldCalculateCorrectEndDate(BudgetPeriod period)
    {
        // Arrange
        var startDate = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = new CreateBudgetCommand(
            Name: "Budget",
            Amount: 1000m,
            Currency: "USD",
            Period: period,
            StartDate: startDate,
            CategoryId: null,
            WarningThreshold: 80m
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.StartDate.Should().Be(startDate);
        result.Value.EndDate.Should().BeAfter(startDate);
    }
}
