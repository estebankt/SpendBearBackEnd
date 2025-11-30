using Notifications.Domain.Enums;
using SpendBear.SharedKernel;

namespace Notifications.Domain.Events;

public sealed record NotificationFailedEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel,
    string Reason
) : DomainEvent();
