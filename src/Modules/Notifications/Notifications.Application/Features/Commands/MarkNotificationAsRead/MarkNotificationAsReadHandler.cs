using Notifications.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Notifications.Application.Features.Commands.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadHandler
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationAsReadHandler(
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        MarkNotificationAsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notificationRepository.GetByIdAsync(
            command.NotificationId,
            cancellationToken);

        if (notification is null)
            return Result.Failure(new Error(
                "Notification.NotFound",
                $"Notification with ID {command.NotificationId} was not found"));

        if (notification.UserId != command.UserId)
            return Result.Failure(new Error(
                "Notification.Unauthorized",
                "You are not authorized to access this notification"));

        notification.MarkAsRead();

        await _notificationRepository.UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
