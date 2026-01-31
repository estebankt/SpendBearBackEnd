using FluentAssertions;
using SpendBear.SharedKernel;
using Spending.Domain.Entities;
using Spending.Domain.Events;
using Spending.Domain.ValueObjects;

namespace Spending.Domain.Tests.Entities;

public class TransactionTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var date = DateTime.UtcNow;
        var description = "Test transaction";
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = TransactionType.Expense;

        // Act
        var result = Transaction.Create(money, date, description, categoryId, userId, type);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(money);
        result.Value.Date.Should().Be(date);
        result.Value.Description.Should().Be(description);
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.UserId.Should().Be(userId);
        result.Value.Type.Should().Be(type);
    }

    [Fact]
    public void Create_WithNullAmount_ShouldFail()
    {
        // Arrange
        var date = DateTime.UtcNow;
        var description = "Test transaction";
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = TransactionType.Expense;

        // Act
        var result = Transaction.Create(null!, date, description, categoryId, userId, type);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.InvalidAmount");
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var date = DateTime.UtcNow;
        var description = "Test transaction";
        var categoryId = Guid.NewGuid();
        var type = TransactionType.Expense;

        // Act
        var result = Transaction.Create(money, date, description, categoryId, Guid.Empty, type);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.InvalidUser");
    }

    [Fact]
    public void Create_WithEmptyCategoryId_ShouldFail()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var date = DateTime.UtcNow;
        var description = "Test transaction";
        var userId = Guid.NewGuid();
        var type = TransactionType.Expense;

        // Act
        var result = Transaction.Create(money, date, description, Guid.Empty, userId, type);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Transaction.InvalidCategory");
    }

    [Fact]
    public void Create_ShouldRaiseTransactionCreatedEvent()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var date = DateTime.UtcNow;
        var description = "Test transaction";
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = TransactionType.Expense;

        // Act
        var result = Transaction.Create(money, date, description, categoryId, userId, type);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().HaveCount(1);
        result.Value.DomainEvents.First().Should().BeOfType<TransactionCreatedEvent>();

        var domainEvent = result.Value.DomainEvents.First() as TransactionCreatedEvent;
        domainEvent!.TransactionId.Should().Be(result.Value.Id);
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Amount.Should().Be(money.Amount);
        domainEvent.Currency.Should().Be(money.Currency);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateProperties()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var date = DateTime.UtcNow;
        var description = "Original description";
        var categoryId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = TransactionType.Expense;
        var transaction = Transaction.Create(money, date, description, categoryId, userId, type).Value;

        var newMoney = Money.Create(200.75m, "EUR").Value;
        var newDate = DateTime.UtcNow.AddDays(1);
        var newDescription = "Updated description";
        var newCategoryId = Guid.NewGuid();
        var newType = TransactionType.Income;

        // Act
        transaction.Update(newMoney, newDate, newDescription, newCategoryId, newType);

        // Assert
        transaction.Amount.Should().Be(newMoney);
        transaction.Date.Should().Be(newDate);
        transaction.Description.Should().Be(newDescription);
        transaction.CategoryId.Should().Be(newCategoryId);
        transaction.Type.Should().Be(newType);
        transaction.UserId.Should().Be(userId); // UserId should not change
    }

    [Fact]
    public void Update_ShouldRaiseTransactionUpdatedEvent()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var transaction = Transaction.Create(
            money,
            DateTime.UtcNow,
            "Test",
            Guid.NewGuid(),
            Guid.NewGuid(),
            TransactionType.Expense
        ).Value;
        transaction.ClearDomainEvents(); // Clear the created event

        var newMoney = Money.Create(200.75m, "EUR").Value;
        var newDate = DateTime.UtcNow.AddDays(1);
        var newDescription = "Updated description";
        var newCategoryId = Guid.NewGuid();
        var newType = TransactionType.Income;

        // Act
        transaction.Update(newMoney, newDate, newDescription, newCategoryId, newType);

        // Assert
        transaction.DomainEvents.Should().HaveCount(1);
        transaction.DomainEvents.First().Should().BeOfType<TransactionUpdatedEvent>();
    }

    [Fact]
    public void Delete_ShouldRaiseTransactionDeletedEvent()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD").Value;
        var userId = Guid.NewGuid();
        var transaction = Transaction.Create(
            money,
            DateTime.UtcNow,
            "Test",
            Guid.NewGuid(),
            userId,
            TransactionType.Expense
        ).Value;
        transaction.ClearDomainEvents(); // Clear the created event

        // Act
        transaction.Delete();

        // Assert
        transaction.DomainEvents.Should().HaveCount(1);
        transaction.DomainEvents.First().Should().BeOfType<TransactionDeletedEvent>();

        var domainEvent = transaction.DomainEvents.First() as TransactionDeletedEvent;
        domainEvent!.TransactionId.Should().Be(transaction.Id);
        domainEvent.UserId.Should().Be(userId);
    }
}
