namespace Analytics.Application.DTOs;

public record MonthlySummaryDto(
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetBalance,
    Dictionary<Guid, decimal> SpendingByCategory,
    Dictionary<Guid, decimal> IncomeByCategory
);
