using FluentAssertions;
using StatementImport.Domain.Entities;

namespace StatementImport.Domain.Tests.Entities;

public class ParsedTransactionTests
{
    [Fact]
    public void EffectiveCategoryId_WithNoConfirmedCategory_ShouldReturnSuggested()
    {
        var suggestedId = Guid.NewGuid();
        var transaction = new ParsedTransaction(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Test",
            25.00m,
            "USD",
            suggestedId,
            null);

        transaction.EffectiveCategoryId.Should().Be(suggestedId);
        transaction.ConfirmedCategoryId.Should().BeNull();
    }

    [Fact]
    public void EffectiveCategoryId_WithConfirmedCategory_ShouldReturnConfirmed()
    {
        var suggestedId = Guid.NewGuid();
        var confirmedId = Guid.NewGuid();
        var transaction = new ParsedTransaction(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Test",
            25.00m,
            "USD",
            suggestedId,
            null);

        transaction.UpdateCategory(confirmedId);

        transaction.EffectiveCategoryId.Should().Be(confirmedId);
        transaction.SuggestedCategoryId.Should().Be(suggestedId);
    }

    [Fact]
    public void UpdateCategory_ShouldSetConfirmedCategoryId()
    {
        var newCategoryId = Guid.NewGuid();
        var transaction = new ParsedTransaction(
            Guid.NewGuid(),
            DateTime.UtcNow,
            "Test",
            25.00m,
            "USD",
            Guid.NewGuid(),
            null);

        transaction.UpdateCategory(newCategoryId);

        transaction.ConfirmedCategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var uploadId = Guid.NewGuid();
        var date = DateTime.UtcNow;
        var categoryId = Guid.NewGuid();

        var transaction = new ParsedTransaction(
            uploadId,
            date,
            "Amazon Purchase",
            49.99m,
            "EUR",
            categoryId,
            "12/25 AMAZON.COM 49.99");

        transaction.StatementUploadId.Should().Be(uploadId);
        transaction.Date.Should().Be(date);
        transaction.Description.Should().Be("Amazon Purchase");
        transaction.Amount.Should().Be(49.99m);
        transaction.Currency.Should().Be("EUR");
        transaction.SuggestedCategoryId.Should().Be(categoryId);
        transaction.OriginalText.Should().Be("12/25 AMAZON.COM 49.99");
    }
}
