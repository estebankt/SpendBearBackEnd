using Notifications.Domain.Enums;

namespace Notifications.Application.Features.Queries.GetNotifications;

public sealed record GetNotificationsQuery(
    Guid UserId,
    NotificationStatus? Status = null,
    NotificationType? Type = null,
    bool UnreadOnly = false,
    int PageNumber = 1,
    int PageSize = 50);
