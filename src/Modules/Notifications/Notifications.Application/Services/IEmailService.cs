namespace Notifications.Application.Services;

public interface IEmailService
{
    Task SendBudgetWarningEmailAsync(
        Guid userId,
        string budgetName,
        decimal budgetAmount,
        decimal currentSpent,
        decimal percentageUsed,
        CancellationToken cancellationToken = default);

    Task SendBudgetExceededEmailAsync(
        Guid userId,
        string budgetName,
        decimal budgetAmount,
        decimal currentSpent,
        decimal exceededBy,
        CancellationToken cancellationToken = default);

    Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default);
}
