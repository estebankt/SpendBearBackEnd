using FluentAssertions;
using StatementImport.Domain.Entities;
using StatementImport.Domain.Enums;
using StatementImport.Domain.Events;

namespace StatementImport.Domain.Tests.Entities;

public class StatementUploadTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var result = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf");

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(_userId);
        result.Value.OriginalFileName.Should().Be("statement.pdf");
        result.Value.StoredFilePath.Should().Be("/path/to/file.pdf");
        result.Value.Status.Should().Be(ImportStatus.Uploading);
        result.Value.ParsedTransactions.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldFail()
    {
        var result = StatementUpload.Create(Guid.Empty, "statement.pdf", "/path/to/file.pdf");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.InvalidUser");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidFileName_ShouldFail(string fileName)
    {
        var result = StatementUpload.Create(_userId, fileName, "/path/to/file.pdf");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.InvalidFileName");
    }

    [Fact]
    public void Create_WithNonPdfFile_ShouldFail()
    {
        var result = StatementUpload.Create(_userId, "statement.csv", "/path/to/file.csv");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.InvalidFormat");
    }

    [Fact]
    public void MarkAsParsing_FromUploading_ShouldSucceed()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;

        var result = upload.MarkAsParsing();

        result.IsSuccess.Should().BeTrue();
        upload.Status.Should().Be(ImportStatus.Parsing);
    }

    [Fact]
    public void MarkAsParsing_FromNonUploadingStatus_ShouldFail()
    {
        var upload = CreateUploadInPendingReview();

        var result = upload.MarkAsParsing();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.InvalidStatus");
    }

    [Fact]
    public void CompleteParsing_WithTransactions_ShouldSucceed()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;
        upload.MarkAsParsing();

        var transactions = CreateParsedTransactions(upload.Id, 3);
        var result = upload.CompleteParsing(transactions);

        result.IsSuccess.Should().BeTrue();
        upload.Status.Should().Be(ImportStatus.PendingReview);
        upload.ParsedTransactions.Should().HaveCount(3);
    }

    [Fact]
    public void CompleteParsing_WithEmptyTransactions_ShouldFail()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;
        upload.MarkAsParsing();

        var result = upload.CompleteParsing(new List<ParsedTransaction>());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.NoTransactions");
    }

    [Fact]
    public void UpdateTransactionCategory_InPendingReview_ShouldSucceed()
    {
        var upload = CreateUploadInPendingReview();
        var transactionId = upload.ParsedTransactions.First().Id;
        var newCategoryId = Guid.NewGuid();

        var result = upload.UpdateTransactionCategory(transactionId, newCategoryId);

        result.IsSuccess.Should().BeTrue();
        upload.ParsedTransactions.First().ConfirmedCategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void UpdateTransactionCategory_WithInvalidTransactionId_ShouldFail()
    {
        var upload = CreateUploadInPendingReview();

        var result = upload.UpdateTransactionCategory(Guid.NewGuid(), Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.TransactionNotFound");
    }

    [Fact]
    public void Confirm_InPendingReview_ShouldSucceedAndRaiseEvent()
    {
        var upload = CreateUploadInPendingReview();

        var result = upload.Confirm();

        result.IsSuccess.Should().BeTrue();
        upload.Status.Should().Be(ImportStatus.Confirmed);
        upload.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StatementImportConfirmedEvent>();
    }

    [Fact]
    public void Confirm_InPendingReview_EventShouldContainTransactionData()
    {
        var upload = CreateUploadInPendingReview();

        upload.Confirm();

        var domainEvent = upload.DomainEvents.OfType<StatementImportConfirmedEvent>().Single();
        domainEvent.StatementUploadId.Should().Be(upload.Id);
        domainEvent.UserId.Should().Be(_userId);
        domainEvent.Transactions.Should().HaveCount(upload.ParsedTransactions.Count);
    }

    [Fact]
    public void Confirm_NotInPendingReview_ShouldFail()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;

        var result = upload.Confirm();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.InvalidStatus");
    }

    [Fact]
    public void Cancel_InPendingReview_ShouldSucceed()
    {
        var upload = CreateUploadInPendingReview();

        var result = upload.Cancel();

        result.IsSuccess.Should().BeTrue();
        upload.Status.Should().Be(ImportStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenConfirmed_ShouldFail()
    {
        var upload = CreateUploadInPendingReview();
        upload.Confirm();

        var result = upload.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementUpload.InvalidStatus");
    }

    [Fact]
    public void MarkAsFailed_ShouldSetStatusAndMessage()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;
        upload.MarkAsParsing();

        var result = upload.MarkAsFailed("PDF extraction failed");

        result.IsSuccess.Should().BeTrue();
        upload.Status.Should().Be(ImportStatus.Failed);
        upload.ErrorMessage.Should().Be("PDF extraction failed");
    }

    private StatementUpload CreateUploadInPendingReview()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;
        upload.MarkAsParsing();
        var transactions = CreateParsedTransactions(upload.Id, 2);
        upload.CompleteParsing(transactions);
        return upload;
    }

    private static List<ParsedTransaction> CreateParsedTransactions(Guid uploadId, int count)
    {
        return Enumerable.Range(1, count).Select(i => new ParsedTransaction(
            uploadId,
            DateTime.UtcNow.AddDays(-i),
            $"Transaction {i}",
            i * 10.50m,
            "USD",
            Guid.NewGuid(),
            $"Original text {i}"
        )).ToList();
    }
}
