using Budgets.Domain.Events;
using Notifications.Application.Services;
using Notifications.Domain.Enums;
using Notifications.Application.Abstractions;
using Notifications.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Notifications.Application.Features.EventHandlers;

public sealed class BudgetExceededEventHandler : IEventHandler<BudgetExceededEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly INotificationsUnitOfWork _unitOfWork;

    public BudgetExceededEventHandler(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        INotificationsUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(BudgetExceededEvent @event, CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, string>
        {
            { "BudgetId", @event.BudgetId.ToString() },
            { "BudgetName", @event.BudgetName },
            { "BudgetAmount", @event.BudgetAmount.ToString("F2") },
            { "CurrentSpent", @event.CurrentSpent.ToString("F2") },
            { "ExceededBy", @event.ExceededBy.ToString("F2") }
        };

        var title = $"Budget Exceeded: {@event.BudgetName}";
        var message = $"You have exceeded your budget for {@event.BudgetName}! " +
                     $"Budget: ${@event.BudgetAmount:F2}, Spent: ${@event.CurrentSpent:F2}, " +
                     $"Exceeded by: ${@event.ExceededBy:F2}.";

        var notificationResult = Domain.Entities.Notification.Create(
            @event.UserId,
            NotificationType.BudgetExceeded,
            NotificationChannel.Email,
            title,
            message,
            metadata
        );

        if (notificationResult.IsFailure)
            return;

        var notification = notificationResult.Value;

        try
        {
            await _emailService.SendBudgetExceededEmailAsync(
                @event.UserId,
                @event.BudgetName,
                @event.BudgetAmount,
                @event.CurrentSpent,
                @event.ExceededBy,
                cancellationToken
            );

            notification.MarkAsSent();
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
        }

        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
