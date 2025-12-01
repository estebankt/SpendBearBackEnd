using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spending.Application.Features.Transactions.CreateTransaction;
using Spending.Application.Features.Transactions.UpdateTransaction;
using Spending.Application.Features.Transactions.DeleteTransaction;
using Spending.Application.Features.Transactions.GetTransactions;
using Spending.Domain.Entities;
using System.Security.Claims;
using SpendBear.SharedKernel.Extensions;

namespace Spending.Api.Controllers;

[ApiController]
[Route("api/spending/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly CreateTransactionHandler _createTransactionHandler;
    private readonly UpdateTransactionHandler _updateTransactionHandler;
    private readonly DeleteTransactionHandler _deleteTransactionHandler;
    private readonly GetTransactionsHandler _getTransactionsHandler;

    public TransactionsController(
        CreateTransactionHandler createTransactionHandler,
        UpdateTransactionHandler updateTransactionHandler,
        DeleteTransactionHandler deleteTransactionHandler,
        GetTransactionsHandler getTransactionsHandler)
    {
        _createTransactionHandler = createTransactionHandler;
        _updateTransactionHandler = updateTransactionHandler;
        _deleteTransactionHandler = deleteTransactionHandler;
        _getTransactionsHandler = getTransactionsHandler;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new CreateTransactionCommand(
            request.Amount,
            request.Currency,
            request.Date,
            request.Description,
            request.CategoryId,
            request.Type
        );

        // Validate
        var validationResult = CreateTransactionValidator.Validate(command);
        if (validationResult.IsFailure)
            return BadRequest(validationResult.Error);

        var result = await _createTransactionHandler.Handle(command, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(
            nameof(GetTransactions),
            new { id = result.Value.Id },
            result.Value
        );
    }

    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] TransactionType? type = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var query = new GetTransactionsQuery(
            startDate,
            endDate,
            categoryId,
            type,
            pageNumber,
            pageSize
        );

        var result = await _getTransactionsHandler.Handle(query, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] UpdateTransactionRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new UpdateTransactionCommand(
            id,
            request.Amount,
            request.Currency,
            request.Date,
            request.Description,
            request.CategoryId,
            request.Type
        );

        // Validate
        var validationResult = UpdateTransactionValidator.Validate(command);
        if (validationResult.IsFailure)
            return BadRequest(validationResult.Error);

        var result = await _updateTransactionHandler.Handle(command, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new DeleteTransactionCommand(id);
        var result = await _deleteTransactionHandler.Handle(command, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }
}

public record CreateTransactionRequest(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid CategoryId,
    TransactionType Type
);

public record UpdateTransactionRequest(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid CategoryId,
    TransactionType Type
);
