using Analytics.Application.Abstractions;
using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using SpendBear.SharedKernel;
using Analytics.Domain.Enums;
using Spending.Domain.Events;
using Spending.Domain.Entities; // For TransactionType

namespace Analytics.Application.Features.EventHandlers;

public sealed class TransactionDeletedEventHandler : IEventHandler<TransactionDeletedEvent>
{
    private readonly IAnalyticSnapshotRepository _analyticSnapshotRepository;
    private readonly IAnalyticsUnitOfWork _unitOfWork;

    public TransactionDeletedEventHandler(IAnalyticSnapshotRepository analyticSnapshotRepository, IAnalyticsUnitOfWork unitOfWork)
    {
        _analyticSnapshotRepository = analyticSnapshotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TransactionDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        // For monthly snapshots, calculate the first day of the month
        var snapshotDate = DateOnly.FromDateTime(@event.Date.Date); // The event should ideally carry the date
        var firstDayOfMonth = new DateOnly(snapshotDate.Year, snapshotDate.Month, 1);
        var period = SnapshotPeriod.Monthly; // Focusing on monthly snapshots for now

        var existingSnapshot = await _analyticSnapshotRepository.GetByUserIdAndDateAsync(
            @event.UserId,
            firstDayOfMonth,
            period,
            cancellationToken
        );

        if (existingSnapshot != null)
        {
            // Reverse the impact of the deleted transaction
            if (@event.Type == TransactionType.Income)
            {
                existingSnapshot.RemoveIncome(@event.CategoryId, @event.Amount);
            }
            else // Type was Expense
            {
                existingSnapshot.RemoveExpense(@event.CategoryId, @event.Amount);
            }
            await _analyticSnapshotRepository.UpdateAsync(existingSnapshot, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        // If existingSnapshot is null, it means there was no snapshot for that month,
        // or the transaction was recorded before snapshot tracking began.
    }
}
