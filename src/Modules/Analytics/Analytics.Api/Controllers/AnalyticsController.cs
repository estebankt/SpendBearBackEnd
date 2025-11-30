using Analytics.Application.DTOs;
using Analytics.Application.Features.Queries.GetMonthlySummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http; // Added this
using SpendBear.SharedKernel;
using System.Security.Claims;

namespace Analytics.Api.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly GetMonthlySummaryHandler _getMonthlySummaryHandler;

    public AnalyticsController(GetMonthlySummaryHandler getMonthlySummaryHandler)
    {
        _getMonthlySummaryHandler = getMonthlySummaryHandler;
    }

    [HttpGet("summary/monthly")]
    [ProducesResponseType(typeof(MonthlySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        if (year < 2000 || year > 2100 || month < 1 || month > 12)
        {
            return BadRequest("Invalid year or month");
        }

        var query = new GetMonthlySummaryQuery(userId, month, year);
        var result = await _getMonthlySummaryHandler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
}
