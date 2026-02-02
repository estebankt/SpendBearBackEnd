using StatementImport.Domain.Enums;

namespace StatementImport.Application.DTOs;

public sealed record StatementUploadDto(
    Guid Id,
    string OriginalFileName,
    DateTime UploadedAt,
    ImportStatus Status,
    string? ErrorMessage,
    List<ParsedTransactionDto> ParsedTransactions
);

public sealed record ParsedTransactionDto(
    Guid Id,
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency,
    Guid SuggestedCategoryId,
    Guid? ConfirmedCategoryId,
    Guid EffectiveCategoryId,
    string? OriginalText
);

public sealed record StatementUploadSummaryDto(
    Guid Id,
    string OriginalFileName,
    DateTime UploadedAt,
    ImportStatus Status,
    int TransactionCount
);
