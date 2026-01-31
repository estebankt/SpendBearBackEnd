using Microsoft.Extensions.Logging;
using Notifications.Application.Services;

namespace Notifications.Infrastructure.Services;

internal sealed class FakeEmailService : IEmailService
{
    private readonly ILogger<FakeEmailService> _logger;

    public FakeEmailService(ILogger<FakeEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendBudgetWarningEmailAsync(
        Guid userId,
        string budgetName,
        decimal budgetAmount,
        decimal currentSpent,
        decimal percentageUsed,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[FAKE EMAIL] Budget Warning for User {UserId}: {BudgetName} at {Percentage}% ({CurrentSpent}/{BudgetAmount})",
            userId, budgetName, percentageUsed, currentSpent, budgetAmount);

        return Task.CompletedTask;
    }

    public Task SendBudgetExceededEmailAsync(
        Guid userId,
        string budgetName,
        decimal budgetAmount,
        decimal currentSpent,
        decimal exceededBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[FAKE EMAIL] Budget Exceeded for User {UserId}: {BudgetName} exceeded by ${ExceededBy} ({CurrentSpent}/{BudgetAmount})",
            userId, budgetName, exceededBy, currentSpent, budgetAmount);

        return Task.CompletedTask;
    }

    public Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[FAKE EMAIL] To: {Email}, Subject: {Subject}",
            toEmail, subject);

        return Task.CompletedTask;
    }
}
