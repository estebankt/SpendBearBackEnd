using SpendBear.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Features.Commands.MarkNotificationAsRead;
using Notifications.Application.Features.Queries.GetNotifications;
using Notifications.Domain.Enums;
using System.Security.Claims;

namespace Notifications.Api.Controllers;

/// <summary>
/// User notification management
/// </summary>
[Authorize]
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly GetNotificationsHandler _getNotificationsHandler;
    private readonly MarkNotificationAsReadHandler _markAsReadHandler;

    public NotificationsController(
        GetNotificationsHandler getNotificationsHandler,
        MarkNotificationAsReadHandler markAsReadHandler)
    {
        _getNotificationsHandler = getNotificationsHandler;
        _markAsReadHandler = markAsReadHandler;
    }

    /// <summary>
    /// Get notifications with optional filtering and pagination
    /// </summary>
    /// <param name="status">Filter by notification status (Pending, Read, Dismissed)</param>
    /// <param name="type">Filter by notification type (BudgetWarning, BudgetExceeded, etc.)</param>
    /// <param name="unreadOnly">Show only unread notifications</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of notifications</returns>
    /// <response code="200">Notifications retrieved successfully</response>
    /// <response code="400">Invalid query parameters</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] NotificationStatus? status = null,
        [FromQuery] NotificationType? type = null,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var query = new GetNotificationsQuery(
            userId.Value,
            status,
            type,
            unreadOnly,
            pageNumber,
            pageSize);

        var result = await _getNotificationsHandler.Handle(query, cancellationToken);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Mark a notification as read
    /// </summary>
    /// <param name="id">Notification ID to mark as read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Notification marked as read successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Missing or invalid authentication token</response>
    /// <response code="403">User does not have permission to modify this notification</response>
    /// <response code="404">Notification not found</response>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized();

        var command = new MarkNotificationAsReadCommand(id, userId.Value);
        var result = await _markAsReadHandler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Notification.NotFound")
                return NotFound(result.Error);

            if (result.Error.Code == "Notification.Unauthorized")
                return Forbid();

            return BadRequest(result.Error);
        }

        return NoContent();
    }
}
