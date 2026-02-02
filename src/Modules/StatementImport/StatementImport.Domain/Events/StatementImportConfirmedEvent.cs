using SpendBear.SharedKernel;

namespace StatementImport.Domain.Events;

public sealed record StatementImportConfirmedEvent(
    Guid StatementUploadId,
    Guid UserId,
    List<ConfirmedTransactionData> Transactions
) : DomainEvent();

public sealed record ConfirmedTransactionData(
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency,
    Guid CategoryId
);
