using SpendBear.SharedKernel.Extensions;
using Budgets.Api.Models;
using Budgets.Application.Features.Budgets.CreateBudget;
using Budgets.Application.Features.Budgets.DeleteBudget;
using Budgets.Application.Features.Budgets.GetBudgets;
using Budgets.Application.Features.Budgets.UpdateBudget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Budgets.Api.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
public sealed class BudgetsController : ControllerBase
{
    private readonly CreateBudgetHandler _createBudgetHandler;
    private readonly GetBudgetsHandler _getBudgetsHandler;
    private readonly UpdateBudgetHandler _updateBudgetHandler;
    private readonly DeleteBudgetHandler _deleteBudgetHandler;

    public BudgetsController(
        CreateBudgetHandler createBudgetHandler,
        GetBudgetsHandler getBudgetsHandler,
        UpdateBudgetHandler updateBudgetHandler,
        DeleteBudgetHandler deleteBudgetHandler)
    {
        _createBudgetHandler = createBudgetHandler;
        _getBudgetsHandler = getBudgetsHandler;
        _updateBudgetHandler = updateBudgetHandler;
        _deleteBudgetHandler = deleteBudgetHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBudget([FromBody] CreateBudgetRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new CreateBudgetCommand(
            request.Name,
            request.Amount,
            request.Currency,
            request.Period,
            request.StartDate,
            request.CategoryId,
            request.WarningThreshold
        );

        var validationResult = CreateBudgetValidator.Validate(command);
        if (validationResult.IsFailure)
            return BadRequest(validationResult.Error);

        var result = await _createBudgetHandler.Handle(command, userId.Value);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBudgets), new { id = result.Value.Id }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpGet]
    public async Task<IActionResult> GetBudgets(
        [FromQuery] bool activeOnly = false,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] DateTime? date = null)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var query = new GetBudgetsQuery(activeOnly, categoryId, date);
        var result = await _getBudgetsHandler.Handle(query, userId.Value);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBudget(Guid id, [FromBody] UpdateBudgetRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new UpdateBudgetCommand(
            id,
            request.Name,
            request.Amount,
            request.Period,
            request.StartDate,
            request.CategoryId,
            request.WarningThreshold
        );

        var result = await _updateBudgetHandler.Handle(command, userId.Value);

        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBudget(Guid id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new DeleteBudgetCommand(id);
        var result = await _deleteBudgetHandler.Handle(command, userId.Value);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }
}
