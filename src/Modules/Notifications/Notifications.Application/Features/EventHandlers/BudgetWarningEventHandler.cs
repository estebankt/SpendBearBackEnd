using Budgets.Domain.Events;
using Notifications.Application.Services;
using Notifications.Domain.Enums;
using Notifications.Domain.Repositories;
using SpendBear.SharedKernel;

namespace Notifications.Application.Features.EventHandlers;

public sealed class BudgetWarningEventHandler : IEventHandler<BudgetWarningEvent>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;

    public BudgetWarningEventHandler(
        INotificationRepository notificationRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(BudgetWarningEvent @event, CancellationToken cancellationToken = default)
    {
        var metadata = new Dictionary<string, string>
        {
            { "BudgetId", @event.BudgetId.ToString() },
            { "BudgetName", @event.BudgetName },
            { "BudgetAmount", @event.BudgetAmount.ToString("F2") },
            { "CurrentSpent", @event.CurrentSpent.ToString("F2") },
            { "PercentageUsed", @event.PercentageUsed.ToString("F2") },
            { "ThresholdPercentage", @event.ThresholdPercentage.ToString("F2") }
        };

        var title = $"Budget Warning: {(int)@event.PercentageUsed}% of {@event.BudgetName}";
        var message = $"You have spent ${@event.CurrentSpent:F2} of your ${@event.BudgetAmount:F2} budget for {@event.BudgetName}. " +
                     $"You are at {@event.PercentageUsed:F1}% of your budget limit.";

        var notificationResult = Domain.Entities.Notification.Create(
            @event.UserId,
            NotificationType.BudgetWarning,
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
            await _emailService.SendBudgetWarningEmailAsync(
                @event.UserId,
                @event.BudgetName,
                @event.BudgetAmount,
                @event.CurrentSpent,
                @event.PercentageUsed,
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
