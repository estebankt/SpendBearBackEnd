using Notifications.Application.DTOs;
using Notifications.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Notifications.Application.Features.Queries.GetNotifications;

public sealed class GetNotificationsHandler
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationsHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<PagedResult<NotificationDto>>> Handle(
        GetNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(
            query.UserId,
            query.Status,
            query.Type,
            query.UnreadOnly,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var totalCount = await _notificationRepository.GetCountAsync(
            query.UserId,
            query.Status,
            query.Type,
            query.UnreadOnly,
            cancellationToken);

        var dtos = notifications.Select(n => new NotificationDto(
            n.Id,
            n.UserId,
            n.Type,
            n.Channel,
            n.Title,
            n.Message,
            n.Status,
            n.CreatedAt,
            n.SentAt,
            n.ReadAt,
            n.FailureReason
        )).ToList();

        var pagedResult = new PagedResult<NotificationDto>(
            dtos,
            totalCount,
            query.PageNumber,
            query.PageSize
        );

        return Result.Success(pagedResult);
    }
}
