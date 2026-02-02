using SpendBear.SharedKernel;

namespace StatementImport.Domain.Entities;

public class ParsedTransaction : Entity
{
    public Guid StatementUploadId { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public Guid SuggestedCategoryId { get; private set; }
    public Guid? ConfirmedCategoryId { get; private set; }
    public string? OriginalText { get; private set; }

    public Guid EffectiveCategoryId => ConfirmedCategoryId ?? SuggestedCategoryId;

    private ParsedTransaction() { }

    public ParsedTransaction(
        Guid statementUploadId,
        DateTime date,
        string description,
        decimal amount,
        string currency,
        Guid suggestedCategoryId,
        string? originalText)
    {
        StatementUploadId = statementUploadId;
        Date = date;
        Description = description;
        Amount = amount;
        Currency = currency;
        SuggestedCategoryId = suggestedCategoryId;
        OriginalText = originalText;
    }

    public void UpdateCategory(Guid categoryId)
    {
        ConfirmedCategoryId = categoryId;
    }
}
