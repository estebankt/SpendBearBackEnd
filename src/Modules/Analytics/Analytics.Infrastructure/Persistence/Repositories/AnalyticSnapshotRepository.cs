using Microsoft.EntityFrameworkCore;
using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using Analytics.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Analytics.Infrastructure.Persistence.Repositories;

internal sealed class AnalyticSnapshotRepository : IAnalyticSnapshotRepository
{
    private readonly AnalyticsDbContext _context;

    public AnalyticSnapshotRepository(AnalyticsDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task AddAsync(AnalyticSnapshot entity, CancellationToken cancellationToken = default)
    {
        await _context.AnalyticSnapshots.AddAsync(entity, cancellationToken);
    }

    public async Task UpdateAsync(AnalyticSnapshot entity, CancellationToken cancellationToken = default)
    {
        _context.AnalyticSnapshots.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(AnalyticSnapshot entity, CancellationToken cancellationToken = default)
    {
        _context.AnalyticSnapshots.Remove(entity);
        await Task.CompletedTask;
    }

    public async Task<AnalyticSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AnalyticSnapshots
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<AnalyticSnapshot?> GetByUserIdAndDateAsync(
        Guid userId, 
        DateOnly snapshotDate, 
        SnapshotPeriod period, 
        CancellationToken cancellationToken = default)
    {
        return await _context.AnalyticSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId && s.SnapshotDate == snapshotDate && s.Period == period, cancellationToken);
    }

    public async Task<List<AnalyticSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, 
        DateOnly? startDate = null, 
        DateOnly? endDate = null, 
        SnapshotPeriod? period = null, 
        CancellationToken cancellationToken = default)
    {
        var query = _context.AnalyticSnapshots
            .Where(s => s.UserId == userId);

        if (startDate.HasValue)
        {
            query = query.Where(s => s.SnapshotDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(s => s.SnapshotDate <= endDate.Value);
        }

        if (period.HasValue)
        {
            query = query.Where(s => s.Period == period.Value);
        }

        return await query
            .OrderBy(s => s.SnapshotDate)
            .ToListAsync(cancellationToken);
    }
}
