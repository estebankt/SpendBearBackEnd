namespace Notifications.Application.Features.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(
    Guid NotificationId,
    Guid UserId);
