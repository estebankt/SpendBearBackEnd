using FluentAssertions;
using Moq;
using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Application.Features.UploadStatement;
using StatementImport.Domain.Entities;
using StatementImport.Domain.Enums;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Tests.Features;

public class UploadStatementHandlerTests
{
    private readonly Mock<IStatementUploadRepository> _repositoryMock = new();
    private readonly Mock<IStatementImportUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IFileStorageService> _fileStorageMock = new();
    private readonly Mock<IPdfTextExtractor> _pdfExtractorMock = new();
    private readonly Mock<IStatementParsingService> _parsingServiceMock = new();
    private readonly Mock<ICategoryProvider> _categoryProviderMock = new();
    private readonly UploadStatementHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();

    public UploadStatementHandlerTests()
    {
        _handler = new UploadStatementHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _fileStorageMock.Object,
            _pdfExtractorMock.Object,
            _parsingServiceMock.Object,
            _categoryProviderMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnPendingReviewUpload()
    {
        // Arrange
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadStatementCommand(stream, "statement.pdf");
        var categoryId = Guid.NewGuid();

        _fileStorageMock.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), _userId, default))
            .ReturnsAsync(Result.Success("/stored/path.pdf"));

        _pdfExtractorMock.Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), default))
            .ReturnsAsync(Result.Success("Statement text content"));

        _categoryProviderMock.Setup(x => x.GetAvailableCategoriesForUserAsync(_userId, default))
            .ReturnsAsync(new List<CategoryInfo> { new(categoryId, "Groceries", "Food purchases") });

        _parsingServiceMock.Setup(x => x.ParseStatementTextAsync(It.IsAny<string>(), It.IsAny<List<CategoryInfo>>(), default))
            .ReturnsAsync(Result.Success(new List<RawParsedTransaction>
            {
                new(DateTime.UtcNow, "Walmart Purchase", 50.00m, "USD", "Groceries", "WALMART $50.00")
            }));

        _categoryProviderMock.Setup(x => x.GetCategoryIdByNameAsync("Groceries", _userId, default))
            .ReturnsAsync(categoryId);

        // Act
        var result = await _handler.Handle(command, _userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ImportStatus.PendingReview);
        result.Value.ParsedTransactions.Should().HaveCount(1);
        result.Value.ParsedTransactions[0].SuggestedCategoryId.Should().Be(categoryId);

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<StatementUpload>(), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Handle_FileStorageFails_ShouldReturnFailure()
    {
        var stream = new MemoryStream();
        var command = new UploadStatementCommand(stream, "statement.pdf");

        _fileStorageMock.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), _userId, default))
            .ReturnsAsync(Result.Failure<string>(new Error("File.Error", "Storage failed")));

        var result = await _handler.Handle(command, _userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementImport.FileStorageFailed");
    }

    [Fact]
    public async Task Handle_PdfExtractionFails_ShouldReturnFailure()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadStatementCommand(stream, "statement.pdf");

        _fileStorageMock.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), _userId, default))
            .ReturnsAsync(Result.Success("/stored/path.pdf"));

        _pdfExtractorMock.Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), default))
            .ReturnsAsync(Result.Failure<string>(new Error("Pdf.Error", "Cannot extract")));

        var result = await _handler.Handle(command, _userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementImport.PdfExtractionFailed");
    }

    [Fact]
    public async Task Handle_AiParsingFails_ShouldReturnFailure()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadStatementCommand(stream, "statement.pdf");

        _fileStorageMock.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), _userId, default))
            .ReturnsAsync(Result.Success("/stored/path.pdf"));

        _pdfExtractorMock.Setup(x => x.ExtractTextAsync(It.IsAny<Stream>(), default))
            .ReturnsAsync(Result.Success("Statement text"));

        _categoryProviderMock.Setup(x => x.GetAvailableCategoriesForUserAsync(_userId, default))
            .ReturnsAsync(new List<CategoryInfo>());

        _parsingServiceMock.Setup(x => x.ParseStatementTextAsync(It.IsAny<string>(), It.IsAny<List<CategoryInfo>>(), default))
            .ReturnsAsync(Result.Failure<List<RawParsedTransaction>>(new Error("AI.Error", "Parse failed")));

        var result = await _handler.Handle(command, _userId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("StatementImport.AiParsingFailed");
    }
}
