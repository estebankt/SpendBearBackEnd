using Budgets.Domain.Enums;
using Budgets.Domain.Events;
using SpendBear.SharedKernel;

namespace Budgets.Domain.Entities;

public sealed class Budget : AggregateRoot
{
    private Budget(
        Guid id,
        string name,
        decimal amount,
        string currency,
        BudgetPeriod period,
        DateTime startDate,
        DateTime endDate,
        Guid userId,
        Guid? categoryId,
        decimal warningThreshold) : base(id)
    {
        Name = name;
        Amount = amount;
        Currency = currency;
        Period = period;
        StartDate = startDate;
        EndDate = endDate;
        UserId = userId;
        CategoryId = categoryId;
        WarningThreshold = warningThreshold;
        CurrentSpent = 0;
        IsExceeded = false;
        WarningTriggered = false;
    }

    public string Name { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public BudgetPeriod Period { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? CategoryId { get; private set; } // Null = all categories
    public decimal CurrentSpent { get; private set; }
    public decimal WarningThreshold { get; private set; } // Percentage (e.g., 80 for 80%)
    public bool IsExceeded { get; private set; }
    public bool WarningTriggered { get; private set; }

    public decimal RemainingAmount => Amount - CurrentSpent;
    public decimal PercentageUsed => Amount > 0 ? (CurrentSpent / Amount) * 100 : 0;

    public static Result<Budget> Create(
        string name,
        decimal amount,
        string currency,
        BudgetPeriod period,
        DateTime startDate,
        Guid userId,
        Guid? categoryId = null,
        decimal warningThreshold = 80)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Budget>(new Error("Budget.InvalidName", "Budget name cannot be empty"));

        if (amount <= 0)
            return Result.Failure<Budget>(new Error("Budget.InvalidAmount", "Budget amount must be greater than zero"));

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Budget>(new Error("Budget.InvalidCurrency", "Currency must be a 3-letter code"));

        if (userId == Guid.Empty)
            return Result.Failure<Budget>(new Error("Budget.InvalidUser", "User ID cannot be empty"));

        if (warningThreshold < 0 || warningThreshold > 100)
            return Result.Failure<Budget>(new Error("Budget.InvalidThreshold", "Warning threshold must be between 0 and 100"));

        var endDate = CalculateEndDate(startDate, period);

        var budget = new Budget(
            Guid.NewGuid(),
            name,
            amount,
            currency,
            period,
            startDate,
            endDate,
            userId,
            categoryId,
            warningThreshold
        );

        budget.RaiseDomainEvent(new BudgetCreatedEvent(
            budget.Id,
            userId,
            name,
            amount,
            currency,
            categoryId
        ));

        return Result.Success(budget);
    }

    public void Update(
        string name,
        decimal amount,
        BudgetPeriod period,
        DateTime startDate,
        Guid? categoryId,
        decimal warningThreshold)
    {
        Name = name;
        Amount = amount;
        Period = period;
        StartDate = startDate;
        EndDate = CalculateEndDate(startDate, period);
        CategoryId = categoryId;
        WarningThreshold = warningThreshold;

        // Reset warning if threshold changed
        if (WarningTriggered && PercentageUsed < warningThreshold)
        {
            WarningTriggered = false;
        }

        // Recheck if budget is now exceeded with new amount
        if (!IsExceeded && CurrentSpent > Amount)
        {
            IsExceeded = true;
            RaiseDomainEvent(new BudgetExceededEvent(
                Id,
                UserId,
                Name,
                Amount,
                CurrentSpent,
                CurrentSpent - Amount
            ));
        }
        else if (IsExceeded && CurrentSpent <= Amount)
        {
            IsExceeded = false;
        }

        RaiseDomainEvent(new BudgetUpdatedEvent(Id, UserId));
    }

    public void RecordTransaction(decimal transactionAmount)
    {
        CurrentSpent += transactionAmount;

        // Check warning threshold
        if (!WarningTriggered && PercentageUsed >= WarningThreshold)
        {
            WarningTriggered = true;
            RaiseDomainEvent(new BudgetWarningEvent(
                Id,
                UserId,
                Name,
                Amount,
                CurrentSpent,
                PercentageUsed,
                WarningThreshold
            ));
        }

        // Check if exceeded
        if (!IsExceeded && CurrentSpent > Amount)
        {
            IsExceeded = true;
            RaiseDomainEvent(new BudgetExceededEvent(
                Id,
                UserId,
                Name,
                Amount,
                CurrentSpent,
                CurrentSpent - Amount
            ));
        }
    }

    public void ResetForNewPeriod(DateTime newStartDate)
    {
        CurrentSpent = 0;
        IsExceeded = false;
        WarningTriggered = false;
        StartDate = newStartDate;
        EndDate = CalculateEndDate(newStartDate, Period);
    }

    public bool IsInPeriod(DateTime date)
    {
        return date >= StartDate && date <= EndDate;
    }

    private static DateTime CalculateEndDate(DateTime startDate, BudgetPeriod period)
    {
        return period switch
        {
            BudgetPeriod.Daily => startDate.AddDays(1).AddSeconds(-1),
            BudgetPeriod.Weekly => startDate.AddDays(7).AddSeconds(-1),
            BudgetPeriod.Monthly => startDate.AddMonths(1).AddSeconds(-1),
            BudgetPeriod.Yearly => startDate.AddYears(1).AddSeconds(-1),
            _ => throw new ArgumentException($"Unknown budget period: {period}")
        };
    }
}
