using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notifications.Application.Services;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Notifications.Infrastructure.Services;

internal sealed class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _sendGridClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendGridEmailService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(
        ISendGridClient sendGridClient,
        IConfiguration configuration,
        ILogger<SendGridEmailService> logger)
    {
        _sendGridClient = sendGridClient;
        _configuration = configuration;
        _logger = logger;
        _fromEmail = configuration["SendGrid:FromEmail"] ?? "notifications@spendbear.com";
        _fromName = configuration["SendGrid:FromName"] ?? "SpendBear";
    }

    public async Task SendBudgetWarningEmailAsync(
        Guid userId,
        string budgetName,
        decimal budgetAmount,
        decimal currentSpent,
        decimal percentageUsed,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Budget Warning: {(int)percentageUsed}% of {budgetName}";

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FFA500; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid: #ffc107; padding: 15px; margin: 20px 0; }}
        .progress-bar {{ background-color: #e0e0e0; height: 30px; border-radius: 15px; overflow: hidden; }}
        .progress-fill {{ background-color: #ffc107; height: 100%; text-align: center; line-height: 30px; color: white; font-weight: bold; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Budget Warning</h1>
        </div>
        <div class='content'>
            <div class='warning'>
                <h2>{budgetName}</h2>
                <p>You've reached <strong>{percentageUsed:F1}%</strong> of your budget limit.</p>
            </div>

            <div class='progress-bar'>
                <div class='progress-fill' style='width: {percentageUsed:F0}%'>{percentageUsed:F0}%</div>
            </div>

            <table style='width: 100%; margin-top: 20px;'>
                <tr>
                    <td><strong>Budget Limit:</strong></td>
                    <td style='text-align: right;'>${budgetAmount:F2}</td>
                </tr>
                <tr>
                    <td><strong>Current Spent:</strong></td>
                    <td style='text-align: right;'>${currentSpent:F2}</td>
                </tr>
                <tr>
                    <td><strong>Remaining:</strong></td>
                    <td style='text-align: right;'>${(budgetAmount - currentSpent):F2}</td>
                </tr>
            </table>

            <p style='margin-top: 20px;'>Consider reviewing your spending to stay within your budget.</p>
        </div>
        <div class='footer'>
            <p>This is an automated notification from SpendBear.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(
            GetUserEmail(userId),
            subject,
            htmlContent,
            cancellationToken);
    }

    public async Task SendBudgetExceededEmailAsync(
        Guid userId,
        string budgetName,
        decimal budgetAmount,
        decimal currentSpent,
        decimal exceededBy,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Budget Exceeded: {budgetName}";

        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; background-color: #f9f9f9; }}
        .alert {{ background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; }}
        .exceeded-amount {{ font-size: 24px; color: #dc3545; font-weight: bold; text-align: center; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🚨 Budget Exceeded</h1>
        </div>
        <div class='content'>
            <div class='alert'>
                <h2>{budgetName}</h2>
                <p>You have exceeded your budget limit!</p>
            </div>

            <div class='exceeded-amount'>
                Over budget by ${exceededBy:F2}
            </div>

            <table style='width: 100%; margin-top: 20px;'>
                <tr>
                    <td><strong>Budget Limit:</strong></td>
                    <td style='text-align: right;'>${budgetAmount:F2}</td>
                </tr>
                <tr style='color: #dc3545;'>
                    <td><strong>Current Spent:</strong></td>
                    <td style='text-align: right; font-weight: bold;'>${currentSpent:F2}</td>
                </tr>
                <tr style='color: #dc3545;'>
                    <td><strong>Exceeded By:</strong></td>
                    <td style='text-align: right; font-weight: bold;'>${exceededBy:F2}</td>
                </tr>
                <tr>
                    <td><strong>Percentage:</strong></td>
                    <td style='text-align: right;'>{(currentSpent / budgetAmount * 100):F1}%</td>
                </tr>
            </table>

            <p style='margin-top: 20px; color: #dc3545; font-weight: bold;'>
                ⚠️ Please review your spending and consider adjusting your budget or reducing expenses.
            </p>
        </div>
        <div class='footer'>
            <p>This is an automated notification from SpendBear.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(
            GetUserEmail(userId),
            subject,
            htmlContent,
            cancellationToken);
    }

    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken = default)
    {
        var from = new EmailAddress(_fromEmail, _fromName);
        var to = new EmailAddress(toEmail);
        var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);

        try
        {
            var response = await _sendGridClient.SendEmailAsync(msg, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            else
            {
                var body = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to send email to {Email}. Status: {StatusCode}, Body: {Body}",
                    toEmail, response.StatusCode, body);
                throw new Exception($"Failed to send email: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            throw;
        }
    }

    private string GetUserEmail(Guid userId)
    {
        // TODO: In production, fetch user email from Identity module/database
        // For now, return a placeholder
        return $"user-{userId}@example.com";
    }
}
