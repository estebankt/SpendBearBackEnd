using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SpendBear.SharedKernel;
using SpendBear.SharedKernel.Extensions;
using StatementImport.Application.Features.CancelImport;
using StatementImport.Application.Features.ConfirmImport;
using StatementImport.Application.Features.GetPendingImport;
using StatementImport.Application.Features.GetUserImports;
using StatementImport.Application.Features.UpdateParsedTransactions;
using StatementImport.Application.Features.UploadStatement;

namespace StatementImport.Api.Controllers;

/// <summary>
/// Credit card statement import management
/// </summary>
[ApiController]
[Route("api/statement-import")]
[Authorize]
public class StatementImportController : ControllerBase
{
    private readonly UploadStatementHandler _uploadHandler;
    private readonly GetPendingImportHandler _getPendingImportHandler;
    private readonly UpdateParsedTransactionsHandler _updateHandler;
    private readonly ConfirmImportHandler _confirmHandler;
    private readonly CancelImportHandler _cancelHandler;
    private readonly GetUserImportsHandler _getUserImportsHandler;

    public StatementImportController(
        UploadStatementHandler uploadHandler,
        GetPendingImportHandler getPendingImportHandler,
        UpdateParsedTransactionsHandler updateHandler,
        ConfirmImportHandler confirmHandler,
        CancelImportHandler cancelHandler,
        GetUserImportsHandler getUserImportsHandler)
    {
        _uploadHandler = uploadHandler;
        _getPendingImportHandler = getPendingImportHandler;
        _updateHandler = updateHandler;
        _confirmHandler = confirmHandler;
        _cancelHandler = cancelHandler;
        _getUserImportsHandler = getUserImportsHandler;
    }

    /// <summary>
    /// Upload a credit card statement PDF for parsing
    /// </summary>
    /// <param name="file">PDF file of the credit card statement</param>
    /// <returns>The parsed statement with AI-categorized transactions</returns>
    /// <response code="201">Statement parsed successfully, pending review</response>
    /// <response code="400">Invalid file or parsing failed</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadStatement(IFormFile file)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        if (file == null || file.Length == 0)
            return BadRequest(new Error("Upload.NoFile", "A PDF file is required."));

        if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new Error("Upload.InvalidFormat", "Only PDF files are accepted."));

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new Error("Upload.TooLarge", "File size cannot exceed 10MB."));

        using var stream = file.OpenReadStream();
        var command = new UploadStatementCommand(stream, file.FileName);
        var result = await _uploadHandler.Handle(command, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetPendingImport), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Get a statement import by ID with all parsed transactions
    /// </summary>
    /// <param name="id">Statement upload ID</param>
    /// <returns>The statement upload with parsed transactions</returns>
    /// <response code="200">Statement retrieved successfully</response>
    /// <response code="400">Statement not found or not authorized</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPendingImport(Guid id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var result = await _getPendingImportHandler.Handle(id, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Update categories on parsed transactions
    /// </summary>
    /// <param name="id">Statement upload ID</param>
    /// <param name="request">List of transaction category updates</param>
    /// <returns>The updated statement with transactions</returns>
    /// <response code="200">Transactions updated successfully</response>
    /// <response code="400">Invalid data or wrong status</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpPut("{id}/transactions")]
    public async Task<IActionResult> UpdateParsedTransactions(Guid id, [FromBody] UpdateTransactionsRequest request)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var command = new UpdateParsedTransactionsCommand(
            id,
            request.Updates.Select(u => new TransactionCategoryUpdate(u.ParsedTransactionId, u.NewCategoryId)).ToList()
        );

        var result = await _updateHandler.Handle(command, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Confirm the import, creating transactions in the Spending module
    /// </summary>
    /// <param name="id">Statement upload ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Import confirmed and transactions created</response>
    /// <response code="400">Import not found, wrong status, or transaction creation failed</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmImport(Guid id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var result = await _confirmHandler.Handle(id, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// Cancel a pending import
    /// </summary>
    /// <param name="id">Statement upload ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Import cancelled</response>
    /// <response code="400">Import not found or cannot be cancelled</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelImport(Guid id)
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var result = await _cancelHandler.Handle(id, userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return NoContent();
    }

    /// <summary>
    /// List all statement imports for the current user
    /// </summary>
    /// <returns>List of statement upload summaries</returns>
    /// <response code="200">Imports retrieved successfully</response>
    /// <response code="401">Missing or invalid authentication token</response>
    [HttpGet]
    public async Task<IActionResult> GetUserImports()
    {
        var userId = User.GetUserId();
        if (userId == null)
            return Unauthorized(new { Error = "User ID not found in token" });

        var result = await _getUserImportsHandler.Handle(userId.Value);

        if (result.IsFailure)
            return BadRequest(result.Error);

        return Ok(result.Value);
    }
}

/// <summary>
/// Request to update parsed transaction categories
/// </summary>
public record UpdateTransactionsRequest(List<TransactionUpdateItem> Updates);

/// <summary>
/// Individual transaction category update
/// </summary>
public record TransactionUpdateItem(Guid ParsedTransactionId, Guid NewCategoryId);
