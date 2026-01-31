using Notifications.Domain.Enums;
using SpendBear.SharedKernel;

namespace Notifications.Domain.Events;

public sealed record NotificationCreatedEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationType Type,
    NotificationChannel Channel
) : DomainEvent();
