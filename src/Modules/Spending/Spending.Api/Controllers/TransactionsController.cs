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

/// <summary>
/// Financial transaction management (income and expenses)
/// </summary>
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

    /// <summary>
    /// Create a new financial transaction (income or expense)
    /// </summary>
    /// <param name="request">Transaction details including amount, date, category, and type</param>
    /// <returns>The newly created transaction</returns>
    /// <response code="201">Transaction created successfully</response>
    /// <response code="400">Invalid transaction data</response>
    /// <response code="401">Missing or invalid authentication token</response>
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

    /// <summary>
    /// Get transactions with optional filtering and pagination
    /// </summary>
    /// <param name="startDate">Filter by transactions on or after this date</param>
    /// <param name="endDate">Filter by transactions on or before this date</param>
    /// <param name="categoryId">Filter by specific category</param>
    /// <param name="type">Filter by transaction type (Income or Expense)</param>
    /// <param name="pageNumber">Page number for pagination (default: 1)</param>
    /// <param name="pageSize">Number of items per page (default: 50, max: 100)</param>
    /// <returns>Paginated list of transactions</returns>
    /// <response code="200">Transactions retrieved successfully</response>
    /// <response code="400">Invalid query parameters</response>
    /// <response code="401">Missing or invalid authentication token</response>
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

    /// <summary>
    /// Update an existing transaction
    /// </summary>
    /// <param name="id">Transaction ID</param>
    /// <param name="request">Updated transaction details</param>
    /// <returns>The updated transaction</returns>
    /// <response code="200">Transaction updated successfully</response>
    /// <response code="400">Invalid transaction data or transaction not found</response>
    /// <response code="401">Missing or invalid authentication token</response>
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

    /// <summary>
    /// Delete a transaction
    /// </summary>
    /// <param name="id">Transaction ID to delete</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Transaction deleted successfully</response>
    /// <response code="400">Transaction not found or cannot be deleted</response>
    /// <response code="401">Missing or invalid authentication token</response>
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

/// <summary>
/// Request to create a new transaction
/// </summary>
/// <param name="Amount">Transaction amount (positive for income, positive for expense)</param>
/// <param name="Currency">Currency code (e.g., USD, EUR)</param>
/// <param name="Date">Transaction date</param>
/// <param name="Description">Transaction description</param>
/// <param name="CategoryId">Category ID this transaction belongs to</param>
/// <param name="Type">Transaction type (Income or Expense)</param>
public record CreateTransactionRequest(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid CategoryId,
    TransactionType Type
);

/// <summary>
/// Request to update an existing transaction
/// </summary>
/// <param name="Amount">Updated transaction amount</param>
/// <param name="Currency">Updated currency code</param>
/// <param name="Date">Updated transaction date</param>
/// <param name="Description">Updated description</param>
/// <param name="CategoryId">Updated category ID</param>
/// <param name="Type">Updated transaction type</param>
public record UpdateTransactionRequest(
    decimal Amount,
    string Currency,
    DateTime Date,
    string Description,
    Guid CategoryId,
    TransactionType Type
);
