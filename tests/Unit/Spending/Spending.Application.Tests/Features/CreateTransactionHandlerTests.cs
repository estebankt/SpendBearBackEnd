using FluentAssertions;
using Moq;
using Spending.Application.Abstractions;
using SpendBear.SharedKernel;
using Spending.Application.Features.Transactions.CreateTransaction;
using Spending.Domain.Entities;
using Spending.Domain.Repositories;

namespace Spending.Application.Tests.Features;

public class CreateTransactionHandlerTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<ISpendingUnitOfWork> _mockUnitOfWork;
    private readonly CreateTransactionHandler _handler;

    public CreateTransactionHandlerTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockUnitOfWork = new Mock<ISpendingUnitOfWork>();
        _handler = new CreateTransactionHandler(_mockRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            Amount: 100.50m,
            Currency: "USD",
            Date: DateTime.UtcNow,
            Description: "Test transaction",
            CategoryId: Guid.NewGuid(),
            Type: TransactionType.Expense
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(command.Amount);
        result.Value.Currency.Should().Be(command.Currency);
        result.Value.Description.Should().Be(command.Description);

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidCurrency_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            Amount: 100.50m,
            Currency: "",
            Date: DateTime.UtcNow,
            Description: "Test transaction",
            CategoryId: Guid.NewGuid(),
            Type: TransactionType.Expense
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrency");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyCategoryId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            Amount: 100.50m,
            Currency: "USD",
            Date: DateTime.UtcNow,
            Description: "Test transaction",
            CategoryId: Guid.Empty,
            Type: TransactionType.Expense
        );
        var userId = Guid.NewGuid();

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.InvalidCategory");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateTransactionCommand(
            Amount: 100.50m,
            Currency: "USD",
            Date: DateTime.UtcNow,
            Description: "Test transaction",
            CategoryId: Guid.NewGuid(),
            Type: TransactionType.Expense
        );
        var userId = Guid.Empty;

        // Act
        var result = await _handler.Handle(command, userId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.InvalidUser");
    }
}
