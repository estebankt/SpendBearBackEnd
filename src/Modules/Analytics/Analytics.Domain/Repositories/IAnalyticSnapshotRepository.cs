using Analytics.Domain.Entities;
using SpendBear.SharedKernel;
using Analytics.Domain.Enums;

namespace Analytics.Domain.Repositories;

public interface IAnalyticSnapshotRepository : IRepository<AnalyticSnapshot>
{
    Task<AnalyticSnapshot?> GetByUserIdAndDateAsync(
        Guid userId, 
        DateOnly snapshotDate, 
        SnapshotPeriod period, 
        CancellationToken cancellationToken = default);

    Task<List<AnalyticSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, 
        DateOnly? startDate = null, 
        DateOnly? endDate = null, 
        SnapshotPeriod? period = null, 
        CancellationToken cancellationToken = default);
}
