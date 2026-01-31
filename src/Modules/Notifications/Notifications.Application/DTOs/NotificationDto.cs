using Notifications.Domain.Enums;

namespace Notifications.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid UserId,
    NotificationType Type,
    NotificationChannel Channel,
    string Title,
    string Message,
    NotificationStatus Status,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? ReadAt,
    string? FailureReason);
