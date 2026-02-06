using Notifications.Domain.Entities;
using Notifications.Domain.Enums;
using SpendBear.SharedKernel;

namespace Notifications.Domain.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<List<Notification>> GetByUserIdAsync(
        Guid userId,
        NotificationStatus? status = null,
        NotificationType? type = null,
        bool unreadOnly = false,
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        Guid userId,
        NotificationStatus? status = null,
        NotificationType? type = null,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default);

    Task<List<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
}
