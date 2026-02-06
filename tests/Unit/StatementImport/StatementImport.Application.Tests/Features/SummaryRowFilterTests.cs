using FluentAssertions;
using StatementImport.Application.Abstractions;
using StatementImport.Application.Features.UploadStatement;

namespace StatementImport.Application.Tests.Features;

public class SummaryRowFilterTests
{
    [Theory]
    [InlineData("TOTAL PURCHASES")]
    [InlineData("Total This Period")]
    [InlineData("TOTAL FEES CHARGED")]
    [InlineData("SUBTOTAL")]
    [InlineData("Previous Balance")]
    [InlineData("New Balance")]
    [InlineData("Closing Balance")]
    [InlineData("MINIMUM PAYMENT DUE")]
    [InlineData("Payment Due Date: 01/15/2026")]
    [InlineData("FINANCE CHARGE")]
    [InlineData("Interest Charge")]
    [InlineData("LATE FEE")]
    [InlineData("Annual Fee")]
    [InlineData("Credit Limit: $5,000.00")]
    [InlineData("Available Credit")]
    [InlineData("Year-to-Date Totals")]
    [InlineData("YTD Interest")]
    [InlineData("Payment Received - Thank You")]
    [InlineData("AUTOPAY PAYMENT")]
    public void IsSummaryRow_SummaryDescription_ReturnsTrue(string description)
    {
        var transaction = new RawParsedTransaction(
            DateTime.UtcNow, description, 100.00m, "USD", "Miscellaneous", null);

        UploadStatementHandler.IsSummaryRow(transaction).Should().BeTrue();
    }

    [Theory]
    [InlineData("WALMART SUPERCENTER")]
    [InlineData("Starbucks Coffee")]
    [InlineData("AMAZON.COM")]
    [InlineData("TARGET")]
    [InlineData("Trader Joe's")]
    [InlineData("Chipotle Mexican Grill")]
    [InlineData("Shell Gas Station")]
    [InlineData("Netflix Subscription")]
    [InlineData("Uber Trip")]
    public void IsSummaryRow_RealTransaction_ReturnsFalse(string description)
    {
        var transaction = new RawParsedTransaction(
            DateTime.UtcNow, description, 50.00m, "USD", "Groceries", null);

        UploadStatementHandler.IsSummaryRow(transaction).Should().BeFalse();
    }

    [Fact]
    public void IsSummaryRow_SummaryInOriginalText_ReturnsTrue()
    {
        var transaction = new RawParsedTransaction(
            DateTime.UtcNow, "Some Description", 1500.00m, "USD", "Miscellaneous",
            "TOTAL PURCHASES FOR THIS PERIOD $1,500.00");

        UploadStatementHandler.IsSummaryRow(transaction).Should().BeTrue();
    }

    [Fact]
    public void IsSummaryRow_CleanDescriptionButSummaryOriginal_ReturnsTrue()
    {
        var transaction = new RawParsedTransaction(
            DateTime.UtcNow, "Previous Balance", 2345.67m, "USD", "Miscellaneous",
            "Previous Balance $2,345.67");

        UploadStatementHandler.IsSummaryRow(transaction).Should().BeTrue();
    }

    [Fact]
    public void IsSummaryRow_NullOriginalText_UsesDescriptionOnly()
    {
        var transaction = new RawParsedTransaction(
            DateTime.UtcNow, "WALMART SUPERCENTER", 50.00m, "USD", "Groceries", null);

        UploadStatementHandler.IsSummaryRow(transaction).Should().BeFalse();
    }
}
