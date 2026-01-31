using Microsoft.EntityFrameworkCore;
using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using Notifications.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Notifications.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationsDbContext _context;

    public NotificationRepository(NotificationsDbContext context)
    {
        _context = context;
    }

    public IUnitOfWork UnitOfWork => _context;

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<List<Notification>> GetByUserIdAsync(
        Guid userId,
        NotificationStatus? status = null,
        NotificationType? type = null,
        bool unreadOnly = false,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        if (unreadOnly)
            query = query.Where(n => n.Status != NotificationStatus.Read);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        Guid userId,
        NotificationStatus? status = null,
        NotificationType? type = null,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.Where(n => n.UserId == userId);

        if (status.HasValue)
            query = query.Where(n => n.Status == status.Value);

        if (type.HasValue)
            query = query.Where(n => n.Type == type.Value);

        if (unreadOnly)
            query = query.Where(n => n.Status != NotificationStatus.Read);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetUnreadByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId && n.Status != NotificationStatus.Read)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.Status != NotificationStatus.Read, cancellationToken);
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Remove(notification);
        await Task.CompletedTask;
    }
}