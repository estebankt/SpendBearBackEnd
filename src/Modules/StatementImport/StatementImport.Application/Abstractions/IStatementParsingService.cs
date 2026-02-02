using SpendBear.SharedKernel;

namespace StatementImport.Application.Abstractions;

public interface IStatementParsingService
{
    Task<Result<List<RawParsedTransaction>>> ParseStatementTextAsync(
        string statementText,
        List<CategoryInfo> availableCategories,
        CancellationToken cancellationToken = default);
}

public sealed record RawParsedTransaction(
    DateTime Date,
    string Description,
    decimal Amount,
    string Currency,
    string SuggestedCategoryName,
    string? OriginalText
);

public sealed record CategoryInfo(Guid Id, string Name, string? Description);
