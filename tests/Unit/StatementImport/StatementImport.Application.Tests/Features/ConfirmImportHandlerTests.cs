using FluentAssertions;
using Moq;
using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Application.Features.ConfirmImport;
using StatementImport.Domain.Entities;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Tests.Features;

public class ConfirmImportHandlerTests
{
    private readonly Mock<IStatementUploadRepository> _repositoryMock = new();
    private readonly Mock<IStatementImportUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ITransactionCreationService> _transactionServiceMock = new();
    private readonly ConfirmImportHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public ConfirmImportHandlerTests()
    {
        _handler = new ConfirmImportHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _transactionServiceMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldConfirmAndCreateTransactions()
    {
        var upload = CreateUploadInPendingReview();

        _repositoryMock.Setup(x => x.GetByIdWithTransactionsAsync(upload.Id, default))
            .ReturnsAsync(upload);

        _transactionServiceMock.Setup(x => x.CreateTransactionAsync(
                _userId, It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<Guid>(), default))
            .ReturnsAsync(Result.Success());

        var result = await _handler.Handle(upload.Id, _userId);

        result.IsSuccess.Should().BeTrue();
        _transactionServiceMock.Verify(x => x.CreateTransactionAsync(
            _userId, It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<string>(), It.IsAny<Guid>(), default), Times.Exactly(2));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_UploadNotFound_ShouldFail()
    {
        _repositoryMock.Setup(x => x.GetByIdWithTransactionsAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((StatementUpload?)null);

        var result = await _handler.Handle(Guid.NewGuid(), _userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementImport.NotFound");
    }

    [Fact]
    public async Task Handle_WrongUser_ShouldFail()
    {
        var upload = CreateUploadInPendingReview();

        _repositoryMock.Setup(x => x.GetByIdWithTransactionsAsync(upload.Id, default))
            .ReturnsAsync(upload);

        var result = await _handler.Handle(upload.Id, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementImport.NotAuthorized");
    }

    [Fact]
    public async Task Handle_TransactionCreationFails_ShouldFail()
    {
        var upload = CreateUploadInPendingReview();

        _repositoryMock.Setup(x => x.GetByIdWithTransactionsAsync(upload.Id, default))
            .ReturnsAsync(upload);

        _transactionServiceMock.Setup(x => x.CreateTransactionAsync(
                _userId, It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<Guid>(), default))
            .ReturnsAsync(Result.Failure(new Error("Transaction.Error", "Failed")));

        var result = await _handler.Handle(upload.Id, _userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementImport.TransactionCreationFailed");
    }

    private StatementUpload CreateUploadInPendingReview()
    {
        var upload = StatementUpload.Create(_userId, "statement.pdf", "/path/to/file.pdf").Value;
        upload.MarkAsParsing();
        var transactions = new List<ParsedTransaction>
        {
            new(upload.Id, DateTime.UtcNow, "Purchase 1", 25.00m, "USD", Guid.NewGuid(), null),
            new(upload.Id, DateTime.UtcNow, "Purchase 2", 50.00m, "USD", Guid.NewGuid(), null)
        };
        upload.CompleteParsing(transactions);
        return upload;
    }
}
