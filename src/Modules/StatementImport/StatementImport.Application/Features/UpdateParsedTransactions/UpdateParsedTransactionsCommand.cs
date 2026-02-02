namespace StatementImport.Application.Features.UpdateParsedTransactions;

public sealed record UpdateParsedTransactionsCommand(
    Guid UploadId,
    List<TransactionCategoryUpdate> Updates
);

public sealed record TransactionCategoryUpdate(
    Guid ParsedTransactionId,
    Guid NewCategoryId
);
