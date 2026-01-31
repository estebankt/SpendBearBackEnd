using Notifications.Domain.Enums;
using Notifications.Domain.Events;
using SpendBear.SharedKernel;

namespace Notifications.Domain.Entities;

public sealed class Notification : AggregateRoot
{
    private Notification(
        Guid id,
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string message,
        Dictionary<string, string> metadata) : base(id)
    {
        UserId = userId;
        Type = type;
        Channel = channel;
        Title = title;
        Message = message;
        Metadata = metadata;
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? FailureReason { get; private set; }

    public static Result<Notification> Create(
        Guid userId,
        NotificationType type,
        NotificationChannel channel,
        string title,
        string message,
        Dictionary<string, string>? metadata = null)
    {
        if (userId == Guid.Empty)
            return Result.Failure<Notification>(new Error("Notification.InvalidUser", "User ID cannot be empty"));

        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<Notification>(new Error("Notification.InvalidTitle", "Title cannot be empty"));

        if (string.IsNullOrWhiteSpace(message))
            return Result.Failure<Notification>(new Error("Notification.InvalidMessage", "Message cannot be empty"));

        var notification = new Notification(
            Guid.NewGuid(),
            userId,
            type,
            channel,
            title,
            message,
            metadata ?? new Dictionary<string, string>()
        );

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.Id,
            userId,
            type,
            channel
        ));

        return Result.Success(notification);
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;

        RaiseDomainEvent(new NotificationSentEvent(Id, UserId, Channel));
    }

    public void MarkAsFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;

        RaiseDomainEvent(new NotificationFailedEvent(Id, UserId, Channel, reason));
    }

    public void MarkAsRead()
    {
        if (Status != NotificationStatus.Sent)
            return;

        Status = NotificationStatus.Read;
        ReadAt = DateTime.UtcNow;

        RaiseDomainEvent(new NotificationReadEvent(Id, UserId));
    }
}
