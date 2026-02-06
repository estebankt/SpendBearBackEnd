using SpendBear.SharedKernel;
using Analytics.Domain.Enums;

namespace Analytics.Domain.Entities;

public class AnalyticSnapshot : AggregateRoot
{
    public Guid UserId { get; private set; }
    public DateOnly SnapshotDate { get; private set; } // e.g., first day of the month
    public SnapshotPeriod Period { get; private set; }
    public decimal TotalIncome { get; private set; }
    public decimal TotalExpense { get; private set; }
    public decimal NetBalance { get; private set; }
    public Dictionary<Guid, decimal> SpendingByCategory { get; private set; } = new(); // CategoryId -> Amount
    public Dictionary<Guid, decimal> IncomeByCategory { get; private set; } = new(); // CategoryId -> Amount

    // Private constructor for EF Core and internal use
    private AnalyticSnapshot() { }

    private AnalyticSnapshot(
        Guid id,
        Guid userId,
        DateOnly snapshotDate,
        SnapshotPeriod period,
        decimal totalIncome,
        decimal totalExpense,
        Dictionary<Guid, decimal> spendingByCategory,
        Dictionary<Guid, decimal> incomeByCategory)
        : base(id)
    {
        UserId = userId;
        SnapshotDate = snapshotDate;
        Period = period;
        TotalIncome = totalIncome;
        TotalExpense = totalExpense;
        NetBalance = totalIncome - totalExpense;
        SpendingByCategory = spendingByCategory;
        IncomeByCategory = incomeByCategory;
    }

    public static Result<AnalyticSnapshot> Create(
        Guid userId,
        DateOnly snapshotDate,
        SnapshotPeriod period,
        decimal totalIncome,
        decimal totalExpense,
        Dictionary<Guid, decimal> spendingByCategory,
        Dictionary<Guid, decimal> incomeByCategory)
    {
        if (userId == Guid.Empty)
        {
            return Result.Failure<AnalyticSnapshot>(new Error("AnalyticSnapshot.Create", "UserId cannot be empty."));
        }
        // Additional validation can be added here

        var analyticSnapshot = new AnalyticSnapshot(
            Guid.NewGuid(),
            userId,
            snapshotDate,
            period,
            totalIncome,
            totalExpense,
            spendingByCategory,
            incomeByCategory);

        // Add domain event if needed
        // analyticSnapshot.AddDomainEvent(new AnalyticSnapshotCreatedEvent(analyticSnapshot.Id));

        return Result.Success(analyticSnapshot);
    }

    // Methods to update snapshot data, typically triggered by event handlers
    public void AddIncome(Guid categoryId, decimal amount)
    {
        TotalIncome += amount;
        NetBalance += amount;
        if (IncomeByCategory.ContainsKey(categoryId))
        {
            IncomeByCategory[categoryId] += amount;
        }
        else
        {
            IncomeByCategory.Add(categoryId, amount);
        }
        // Add domain event if needed
    }

    public void AddExpense(Guid categoryId, decimal amount)
    {
        TotalExpense += amount;
        NetBalance -= amount;
        if (SpendingByCategory.ContainsKey(categoryId))
        {
            SpendingByCategory[categoryId] += amount;
        }
        else
        {
            SpendingByCategory.Add(categoryId, amount);
        }
    }

    public void RemoveIncome(Guid categoryId, decimal amount)
    {
        TotalIncome -= amount;
        NetBalance -= amount;
        if (IncomeByCategory.ContainsKey(categoryId))
        {
            IncomeByCategory[categoryId] -= amount;
            if (IncomeByCategory[categoryId] <= 0)
                IncomeByCategory.Remove(categoryId);
        }
    }

    public void RemoveExpense(Guid categoryId, decimal amount)
    {
        TotalExpense -= amount;
        NetBalance += amount;
        if (SpendingByCategory.ContainsKey(categoryId))
        {
            SpendingByCategory[categoryId] -= amount;
            if (SpendingByCategory[categoryId] <= 0)
                SpendingByCategory.Remove(categoryId);
        }
    }
}
