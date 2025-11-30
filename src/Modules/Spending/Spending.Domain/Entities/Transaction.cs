using SpendBear.SharedKernel;
using Spending.Domain.Events;
using Spending.Domain.ValueObjects;

namespace Spending.Domain.Entities;

public class Transaction : AggregateRoot
{
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }

    private Transaction() { }

    private Transaction(Money amount, DateTime date, string description, Guid categoryId, Guid userId, TransactionType type)
    {
        Amount = amount;
        Date = date;
        Description = description;
        CategoryId = categoryId;
        UserId = userId;
        Type = type;
    }

    public static Result<Transaction> Create(Money amount, DateTime date, string description, Guid categoryId, Guid userId, TransactionType type)
    {
        if (amount == null)
            return Result.Failure<Transaction>(new Error("Transaction.InvalidAmount", "Amount is required"));

        if (userId == Guid.Empty)
            return Result.Failure<Transaction>(new Error("Transaction.InvalidUser", "UserId is required"));

        if (categoryId == Guid.Empty)
            return Result.Failure<Transaction>(new Error("Transaction.InvalidCategory", "CategoryId is required"));

        var transaction = new Transaction(amount, date, description, categoryId, userId, type);

        // Raise domain event
        transaction.RaiseDomainEvent(new TransactionCreatedEvent(
            transaction.Id,
            userId,
            amount.Amount,
            amount.Currency,
            type,
            categoryId,
            date
        ));

        return Result.Success(transaction);
    }

    public void Update(Money amount, DateTime date, string description, Guid categoryId, TransactionType type)
    {
        Amount = amount;
        Date = date;
        Description = description;
        CategoryId = categoryId;
        Type = type;

        // Raise domain event
        RaiseDomainEvent(new TransactionUpdatedEvent(
            Id,
            UserId,
            amount.Amount,
            amount.Currency,
            type,
            categoryId,
            date
        ));
    }

    public void Delete()
    {
        // Raise domain event
        RaiseDomainEvent(new TransactionDeletedEvent(
            Id,
            UserId
        ));
    }
}
