using Analytics.Application.Abstractions;
using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using SpendBear.SharedKernel;
using Analytics.Domain.Enums;
using Spending.Domain.Events;
using Spending.Domain.Entities;

namespace Analytics.Application.Features.EventHandlers;

public sealed class TransactionCreatedEventHandler : IEventHandler<TransactionCreatedEvent>
{
    private readonly IAnalyticSnapshotRepository _analyticSnapshotRepository;
    private readonly IAnalyticsUnitOfWork _unitOfWork;

    public TransactionCreatedEventHandler(IAnalyticSnapshotRepository analyticSnapshotRepository, IAnalyticsUnitOfWork unitOfWork)
    {
        _analyticSnapshotRepository = analyticSnapshotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TransactionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        var snapshotDate = DateOnly.FromDateTime(@event.Date.Date);
        var period = SnapshotPeriod.Monthly;

        // For monthly snapshots, always use the first day of the month
        var firstDayOfMonth = new DateOnly(snapshotDate.Year, snapshotDate.Month, 1);

        var existingSnapshot = await _analyticSnapshotRepository.GetByUserIdAndDateAsync(
            @event.UserId,
            firstDayOfMonth,
            period,
            cancellationToken
        );

        if (existingSnapshot == null)
        {
            var newSnapshotResult = AnalyticSnapshot.Create(
                @event.UserId,
                firstDayOfMonth,
                period,
                totalIncome: @event.Type == TransactionType.Income ? @event.Amount : 0, // Corrected
                totalExpense: @event.Type == TransactionType.Expense ? @event.Amount : 0, // Corrected
                spendingByCategory: @event.Type == TransactionType.Expense 
                    ? new Dictionary<Guid, decimal> { { @event.CategoryId, @event.Amount } } // Corrected
                    : new Dictionary<Guid, decimal>(),
                incomeByCategory: @event.Type == TransactionType.Income
                    ? new Dictionary<Guid, decimal> { { @event.CategoryId, @event.Amount } } // Corrected
                    : new Dictionary<Guid, decimal>()
            );

            if (newSnapshotResult.IsFailure)
            {
                // Log error
                return;
            }

            await _analyticSnapshotRepository.AddAsync(newSnapshotResult.Value, cancellationToken);
        }
        else
        {
            if (@event.Type == TransactionType.Income)
            {
                existingSnapshot.AddIncome(@event.CategoryId, @event.Amount); // Corrected
            }
            else // Expense
            {
                existingSnapshot.AddExpense(@event.CategoryId, @event.Amount); // Corrected
            }
            await _analyticSnapshotRepository.UpdateAsync(existingSnapshot, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
