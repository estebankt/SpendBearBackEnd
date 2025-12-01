using SpendBear.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Features.Commands.MarkNotificationAsRead;
using Notifications.Application.Features.Queries.GetNotifications;
using Notifications.Domain.Enums;
using System.Security.Claims;

namespace Notifications.Api.Controllers;

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
