using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using SpendBear.SharedKernel;
using Analytics.Domain.Enums;
using Spending.Domain.Events;
using Spending.Domain.Entities; // For TransactionType

namespace Analytics.Application.Features.EventHandlers;

public sealed class TransactionUpdatedEventHandler : IEventHandler<TransactionUpdatedEvent>
{
    private readonly IAnalyticSnapshotRepository _analyticSnapshotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransactionUpdatedEventHandler(IAnalyticSnapshotRepository analyticSnapshotRepository, IUnitOfWork unitOfWork)
    {
        _analyticSnapshotRepository = analyticSnapshotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(TransactionUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        // For monthly snapshots, calculate the first day of the month for both old and new dates
        var oldSnapshotDate = DateOnly.FromDateTime(@event.OldDate.Date);
        var newSnapshotDate = DateOnly.FromDateTime(@event.Date.Date);
        var period = SnapshotPeriod.Monthly; // Focusing on monthly snapshots for now

        var oldSnapshotFirstDayOfMonth = new DateOnly(oldSnapshotDate.Year, oldSnapshotDate.Month, 1);
        var newSnapshotFirstDayOfMonth = new DateOnly(newSnapshotDate.Year, newSnapshotDate.Month, 1);

        // --- Process Old Transaction Impact ---
        var oldSnapshot = await _analyticSnapshotRepository.GetByUserIdAndDateAsync(
            @event.UserId,
            oldSnapshotFirstDayOfMonth,
            period,
            cancellationToken
        );

        if (oldSnapshot != null)
        {
            if (@event.OldType == TransactionType.Income)
            {
                oldSnapshot.AddExpense(@event.OldCategoryId, @event.OldAmount); // Reverse income by adding as expense
            }
            else // OldType was Expense
            {
                oldSnapshot.AddIncome(@event.OldCategoryId, @event.OldAmount); // Reverse expense by adding as income
            }
            await _analyticSnapshotRepository.UpdateAsync(oldSnapshot, cancellationToken);
        }
        // If oldSnapshot is null, it means there was no snapshot for that month, which is an edge case
        // or implies the initial transaction was before snapshot tracking.

        // --- Process New Transaction Impact ---
        // Check if the transaction moved to a different month
        if (oldSnapshotFirstDayOfMonth == newSnapshotFirstDayOfMonth)
        {
            // Same month, update the same snapshot
            if (oldSnapshot == null) // This can happen if oldSnapshot was never created. Create it now with new data.
            {
                var newSnapshotResult = AnalyticSnapshot.Create(
                    @event.UserId,
                    newSnapshotFirstDayOfMonth,
                    period,
                    totalIncome: @event.Type == TransactionType.Income ? @event.Amount : 0,
                    totalExpense: @event.Type == TransactionType.Expense ? @event.Amount : 0,
                    spendingByCategory: @event.Type == TransactionType.Expense
                        ? new Dictionary<Guid, decimal> { { @event.CategoryId, @event.Amount } }
                        : new Dictionary<Guid, decimal>(),
                    incomeByCategory: @event.Type == TransactionType.Income
                        ? new Dictionary<Guid, decimal> { { @event.CategoryId, @event.Amount } }
                        : new Dictionary<Guid, decimal>()
                );
                if (newSnapshotResult.IsFailure) return; // Log error
                await _analyticSnapshotRepository.AddAsync(newSnapshotResult.Value, cancellationToken);
            }
            else // oldSnapshot is not null and is the same as newSnapshot
            {
                if (@event.Type == TransactionType.Income)
                {
                    oldSnapshot.AddIncome(@event.CategoryId, @event.Amount);
                }
                else // Expense
                {
                    oldSnapshot.AddExpense(@event.CategoryId, @event.Amount);
                }
                await _analyticSnapshotRepository.UpdateAsync(oldSnapshot, cancellationToken);
            }
        }
        else // Transaction moved to a different month
        {
            var newSnapshot = await _analyticSnapshotRepository.GetByUserIdAndDateAsync(
                @event.UserId,
                newSnapshotFirstDayOfMonth,
                period,
                cancellationToken
            );

            if (newSnapshot == null)
            {
                var newSnapshotResult = AnalyticSnapshot.Create(
                    @event.UserId,
                    newSnapshotFirstDayOfMonth,
                    period,
                    totalIncome: @event.Type == TransactionType.Income ? @event.Amount : 0,
                    totalExpense: @event.Type == TransactionType.Expense ? @event.Amount : 0,
                    spendingByCategory: @event.Type == TransactionType.Expense
                        ? new Dictionary<Guid, decimal> { { @event.CategoryId, @event.Amount } }
                        : new Dictionary<Guid, decimal>(),
                    incomeByCategory: @event.Type == TransactionType.Income
                        ? new Dictionary<Guid, decimal> { { @event.CategoryId, @event.Amount } }
                        : new Dictionary<Guid, decimal>()
                );
                if (newSnapshotResult.IsFailure) return; // Log error
                await _analyticSnapshotRepository.AddAsync(newSnapshotResult.Value, cancellationToken);
            }
            else
            {
                if (@event.Type == TransactionType.Income)
                {
                    newSnapshot.AddIncome(@event.CategoryId, @event.Amount);
                }
                else // Expense
                {
                    newSnapshot.AddExpense(@event.CategoryId, @event.Amount);
                }
                await _analyticSnapshotRepository.UpdateAsync(newSnapshot, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
