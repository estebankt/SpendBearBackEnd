using Analytics.Application.DTOs;
using Analytics.Application.Features.Queries.GetMonthlySummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SpendBear.SharedKernel;
using SpendBear.SharedKernel.Extensions;

namespace Analytics.Api.Controllers;

/// <summary>
/// Financial analytics and reporting
/// </summary>
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

    /// <summary>
    /// Get monthly financial summary including income, expenses, and spending by category
    /// </summary>
    /// <param name="year">Year for the summary (2000-2100)</param>
    /// <param name="month">Month for the summary (1-12)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Monthly summary with total income, expenses, net balance, and category breakdowns</returns>
    /// <response code="200">Monthly summary retrieved successfully</response>
    /// <response code="400">Invalid year or month parameters</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpGet("summary/monthly")]
    [ProducesResponseType(typeof(MonthlySummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMonthlySummary([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        if (year < 2000 || year > 2100 || month < 1 || month > 12)
        {
            return BadRequest("Invalid year or month");
        }

        var query = new GetMonthlySummaryQuery(userId.Value, month, year);
        var result = await _getMonthlySummaryHandler.Handle(query, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }
}
