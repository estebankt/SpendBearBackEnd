using SpendBear.SharedKernel;
using StatementImport.Domain.Enums;
using StatementImport.Domain.Events;

namespace StatementImport.Domain.Entities;

public class StatementUpload : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFilePath { get; private set; } = string.Empty;
    public DateTime UploadedAt { get; private set; }
    public ImportStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int? StatementMonth { get; private set; }
    public int? StatementYear { get; private set; }

    private readonly List<ParsedTransaction> _parsedTransactions = new();
    public IReadOnlyCollection<ParsedTransaction> ParsedTransactions => _parsedTransactions.AsReadOnly();

    private StatementUpload() { }

    private StatementUpload(Guid userId, string originalFileName, string storedFilePath)
    {
        UserId = userId;
        OriginalFileName = originalFileName;
        StoredFilePath = storedFilePath;
        UploadedAt = DateTime.UtcNow;
        Status = ImportStatus.Uploading;
    }

    public static Result<StatementUpload> Create(Guid userId, string originalFileName, string storedFilePath)
    {
        if (userId == Guid.Empty)
            return Result.Failure<StatementUpload>(new Error("StatementUpload.InvalidUser", "UserId is required."));

        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result.Failure<StatementUpload>(new Error("StatementUpload.InvalidFileName", "File name is required."));

        if (!originalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return Result.Failure<StatementUpload>(new Error("StatementUpload.InvalidFormat", "Only PDF files are accepted."));

        if (string.IsNullOrWhiteSpace(storedFilePath))
            return Result.Failure<StatementUpload>(new Error("StatementUpload.InvalidPath", "Stored file path is required."));

        return Result.Success(new StatementUpload(userId, originalFileName, storedFilePath));
    }

    public Result MarkAsParsing()
    {
        if (Status != ImportStatus.Uploading)
            return Result.Failure(new Error("StatementUpload.InvalidStatus", "Can only start parsing from Uploading status."));

        Status = ImportStatus.Parsing;
        return Result.Success();
    }

    public Result CompleteParsing(List<ParsedTransaction> transactions)
    {
        if (Status != ImportStatus.Parsing)
            return Result.Failure(new Error("StatementUpload.InvalidStatus", "Can only complete parsing from Parsing status."));

        if (transactions.Count == 0)
            return Result.Failure(new Error("StatementUpload.NoTransactions", "No transactions were found in the statement."));

        _parsedTransactions.AddRange(transactions);
        Status = ImportStatus.PendingReview;
        return Result.Success();
    }

    public Result MarkAsFailed(string errorMessage)
    {
        if (Status == ImportStatus.Confirmed || Status == ImportStatus.Cancelled)
            return Result.Failure(new Error("StatementUpload.InvalidStatus", "Cannot mark a confirmed or cancelled import as failed."));

        Status = ImportStatus.Failed;
        ErrorMessage = errorMessage;
        return Result.Success();
    }

    public Result UpdateTransactionCategory(Guid parsedTransactionId, Guid newCategoryId)
    {
        if (Status != ImportStatus.PendingReview)
            return Result.Failure(new Error("StatementUpload.InvalidStatus", "Can only update transactions while in PendingReview status."));

        var transaction = _parsedTransactions.FirstOrDefault(t => t.Id == parsedTransactionId);
        if (transaction == null)
            return Result.Failure(new Error("StatementUpload.TransactionNotFound", "Parsed transaction not found."));

        transaction.UpdateCategory(newCategoryId);
        return Result.Success();
    }

    public Result Confirm()
    {
        if (Status != ImportStatus.PendingReview)
            return Result.Failure(new Error("StatementUpload.InvalidStatus", "Can only confirm an import that is in PendingReview status."));

        if (_parsedTransactions.Count == 0)
            return Result.Failure(new Error("StatementUpload.NoTransactions", "Cannot confirm an import with no transactions."));

        Status = ImportStatus.Confirmed;

        var confirmedTransactions = _parsedTransactions.Select(t => new ConfirmedTransactionData(
            t.Date,
            t.Description,
            t.Amount,
            t.Currency,
            t.EffectiveCategoryId
        )).ToList();

        RaiseDomainEvent(new StatementImportConfirmedEvent(Id, UserId, confirmedTransactions));

        return Result.Success();
    }

    public Result Cancel()
    {
        if (Status == ImportStatus.Confirmed)
            return Result.Failure(new Error("StatementUpload.InvalidStatus", "Cannot cancel a confirmed import."));

        Status = ImportStatus.Cancelled;
        return Result.Success();
    }
}
