using Analytics.Application.Abstractions;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using Microsoft.Extensions.Logging;
using SpendBear.SharedKernel;

namespace Analytics.Application.Features.Commands.RebuildAnalytics;

public sealed class RebuildAnalyticsHandler
{
    private readonly IAnalyticSnapshotRepository _snapshotRepository;
    private readonly IAnalyticsUnitOfWork _unitOfWork;
    private readonly ITransactionReadService _transactionReadService;
    private readonly ILogger<RebuildAnalyticsHandler> _logger;

    public RebuildAnalyticsHandler(
        IAnalyticSnapshotRepository snapshotRepository,
        IAnalyticsUnitOfWork unitOfWork,
        ITransactionReadService transactionReadService,
        ILogger<RebuildAnalyticsHandler> logger)
    {
        _snapshotRepository = snapshotRepository;
        _unitOfWork = unitOfWork;
        _transactionReadService = transactionReadService;
        _logger = logger;
    }

    public async Task<Result> Handle(RebuildAnalyticsCommand command, CancellationToken cancellationToken)
    {
        var userIds = command.UserId.HasValue
            ? [command.UserId.Value]
            : await _transactionReadService.GetAllUserIdsAsync(cancellationToken);

        _logger.LogInformation("Rebuilding analytics for {Count} user(s)", userIds.Count);

        foreach (var userId in userIds)
        {
            await RebuildForUserAsync(userId, cancellationToken);
        }

        return Result.Success();
    }

    private async Task RebuildForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Delete existing monthly snapshots
        var existingSnapshots = await _snapshotRepository.GetSnapshotsByUserIdAsync(
            userId, period: SnapshotPeriod.Monthly, cancellationToken: cancellationToken);

        foreach (var snapshot in existingSnapshots)
        {
            await _snapshotRepository.DeleteAsync(snapshot, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Stream all transactions and group by month
        var monthlyData = new Dictionary<DateOnly, MonthAccumulator>();

        await foreach (var txn in _transactionReadService.GetAllTransactionsAsync(userId, cancellationToken))
        {
            var monthKey = new DateOnly(txn.Date.Year, txn.Date.Month, 1);

            if (!monthlyData.TryGetValue(monthKey, out var accumulator))
            {
                accumulator = new MonthAccumulator();
                monthlyData[monthKey] = accumulator;
            }

            // TransactionType: 1 = Expense, 2 = Income
            if (txn.Type == 2)
            {
                accumulator.TotalIncome += txn.Amount;
                accumulator.IncomeByCategory.TryGetValue(txn.CategoryId, out var existing);
                accumulator.IncomeByCategory[txn.CategoryId] = existing + txn.Amount;
            }
            else
            {
                accumulator.TotalExpense += txn.Amount;
                accumulator.SpendingByCategory.TryGetValue(txn.CategoryId, out var existing);
                accumulator.SpendingByCategory[txn.CategoryId] = existing + txn.Amount;
            }
        }

        // Create new snapshots
        foreach (var (monthKey, data) in monthlyData)
        {
            var result = AnalyticSnapshot.Create(
                userId,
                monthKey,
                SnapshotPeriod.Monthly,
                data.TotalIncome,
                data.TotalExpense,
                data.SpendingByCategory,
                data.IncomeByCategory);

            if (result.IsSuccess)
            {
                await _snapshotRepository.AddAsync(result.Value, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Rebuilt {Count} monthly snapshots for user {UserId}", monthlyData.Count, userId);
    }

    private sealed class MonthAccumulator
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public Dictionary<Guid, decimal> SpendingByCategory { get; } = new();
        public Dictionary<Guid, decimal> IncomeByCategory { get; } = new();
    }
}
