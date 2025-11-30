using Analytics.Application.DTOs;
using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Analytics.Application.Features.Queries.GetMonthlySummary;

public sealed class GetMonthlySummaryHandler
{
    private readonly IAnalyticSnapshotRepository _repository;

    public GetMonthlySummaryHandler(IAnalyticSnapshotRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<MonthlySummaryDto>> Handle(GetMonthlySummaryQuery query, CancellationToken cancellationToken)
    {
        var snapshotDate = new DateOnly(query.Year, query.Month, 1);
        
        var snapshot = await _repository.GetByUserIdAndDateAsync(
            query.UserId,
            snapshotDate,
            SnapshotPeriod.Monthly,
            cancellationToken
        );

        if (snapshot == null)
        {
            // If no snapshot exists, return empty summary rather than error
            // Or we could return a Not Found error depending on requirements.
            // Returning empty summary is usually better for UX.
            return Result.Success(new MonthlySummaryDto(
                snapshotDate,
                snapshotDate.AddMonths(1).AddDays(-1),
                0,
                0,
                0,
                new Dictionary<Guid, decimal>(),
                new Dictionary<Guid, decimal>()
            ));
        }

        var dto = new MonthlySummaryDto(
            snapshot.SnapshotDate,
            snapshot.SnapshotDate.AddMonths(1).AddDays(-1),
            snapshot.TotalIncome,
            snapshot.TotalExpense,
            snapshot.NetBalance,
            snapshot.SpendingByCategory,
            snapshot.IncomeByCategory
        );

        return Result.Success(dto);
    }
}
