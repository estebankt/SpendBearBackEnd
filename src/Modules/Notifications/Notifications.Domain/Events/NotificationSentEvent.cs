using Notifications.Domain.Enums;
using SpendBear.SharedKernel;

namespace Notifications.Domain.Events;

public sealed record NotificationSentEvent(
    Guid NotificationId,
    Guid UserId,
    NotificationChannel Channel
) : DomainEvent();
