using SpendBear.SharedKernel;

namespace Notifications.Domain.Events;

public sealed record NotificationReadEvent(
    Guid NotificationId,
    Guid UserId
) : DomainEvent();
