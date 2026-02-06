using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Application.DTOs;
using StatementImport.Domain.Entities;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.UploadStatement;

public sealed class UploadStatementHandler
{
    private static readonly string[] SummaryKeywords =
    [
        "total purchases",
        "total this period",
        "total fees",
        "total interest",
        "total charges",
        "subtotal",
        "sub-total",
        "previous balance",
        "new balance",
        "closing balance",
        "opening balance",
        "beginning balance",
        "ending balance",
        "statement balance",
        "minimum payment",
        "minimum due",
        "payment due",
        "amount due",
        "finance charge",
        "interest charge",
        "interest charged",
        "purchase interest",
        "late fee",
        "annual fee",
        "membership fee",
        "over limit fee",
        "overlimit fee",
        "return check fee",
        "returned payment fee",
        "cash advance fee",
        "balance transfer fee",
        "foreign transaction fee",
        "credit limit",
        "available credit",
        "cash advance limit",
        "year-to-date",
        "year to date",
        "ytd totals",
        "ytd interest",
        "ytd fees",
        "promotional balance",
        "deferred interest",
        "account number",
        "statement date",
        "payment received",
        "payment - thank you",
        "autopay payment",
        "total credits",
        "total debits",
        "total payments",
        "rewards summary",
        "points earned",
        "cashback earned"
    ];

    private readonly IStatementUploadRepository _repository;
    private readonly IStatementImportUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorage;
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IStatementParsingService _parsingService;
    private readonly ICategoryProvider _categoryProvider;

    public UploadStatementHandler(
        IStatementUploadRepository repository,
        IStatementImportUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        IPdfTextExtractor pdfExtractor,
        IStatementParsingService parsingService,
        ICategoryProvider categoryProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _pdfExtractor = pdfExtractor;
        _parsingService = parsingService;
        _categoryProvider = categoryProvider;
    }

    public async Task<Result<StatementUploadDto>> Handle(
        UploadStatementCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Save file to storage
        var saveResult = await _fileStorage.SaveFileAsync(command.FileStream, command.FileName, userId, cancellationToken);
        if (saveResult.IsFailure)
            return Result.Failure<StatementUploadDto>(StatementImportErrors.FileStorageFailed);

        // Create aggregate
        var uploadResult = StatementUpload.Create(userId, command.FileName, saveResult.Value);
        if (uploadResult.IsFailure)
            return Result.Failure<StatementUploadDto>(uploadResult.Error);

        var upload = uploadResult.Value;

        // Mark as parsing
        var parsingResult = upload.MarkAsParsing();
        if (parsingResult.IsFailure)
            return Result.Failure<StatementUploadDto>(parsingResult.Error);

        // Extract text from PDF
        command.FileStream.Position = 0;
        var textResult = await _pdfExtractor.ExtractTextAsync(command.FileStream, cancellationToken);
        if (textResult.IsFailure)
        {
            upload.MarkAsFailed(textResult.Error.Message);
            await _repository.AddAsync(upload, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<StatementUploadDto>(StatementImportErrors.PdfExtractionFailed);
        }

        // Get available categories
        var categories = await _categoryProvider.GetAvailableCategoriesForUserAsync(userId, cancellationToken);

        // Parse with AI
        var parseResult = await _parsingService.ParseStatementTextAsync(textResult.Value, categories, cancellationToken);
        if (parseResult.IsFailure)
        {
            upload.MarkAsFailed(parseResult.Error.Message);
            await _repository.AddAsync(upload, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<StatementUploadDto>(StatementImportErrors.AiParsingFailed);
        }

        // Filter out summary rows that the AI may have incorrectly included
        var transactions = parseResult.Value.Where(t => !IsSummaryRow(t)).ToList();

        if (transactions.Count == 0)
        {
            upload.MarkAsFailed("No transactions found in statement.");
            await _repository.AddAsync(upload, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<StatementUploadDto>(StatementImportErrors.NoTransactionsParsed);
        }

        // Resolve the Miscellaneous category by name for fallback
        var miscellaneousCategoryId = categories
            .FirstOrDefault(c => c.Name.Equals("Miscellaneous", StringComparison.OrdinalIgnoreCase))?.Id;
        var fallbackCategoryId = miscellaneousCategoryId ?? (categories.Count > 0 ? categories[0].Id : Guid.Empty);

        // Map raw parsed transactions to domain entities
        var parsedTransactions = new List<ParsedTransaction>();
        foreach (var raw in transactions)
        {
            var categoryId = await _categoryProvider.GetCategoryIdByNameAsync(raw.SuggestedCategoryName, userId, cancellationToken);

            parsedTransactions.Add(new ParsedTransaction(
                upload.Id,
                raw.Date,
                raw.Description,
                raw.Amount,
                raw.Currency,
                categoryId ?? fallbackCategoryId,
                raw.OriginalText));
        }

        // Complete parsing
        var completeResult = upload.CompleteParsing(parsedTransactions);
        if (completeResult.IsFailure)
            return Result.Failure<StatementUploadDto>(completeResult.Error);

        // Persist
        await _repository.AddAsync(upload, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(upload));
    }

    internal static bool IsSummaryRow(RawParsedTransaction transaction)
    {
        var description = transaction.Description;
        var originalText = transaction.OriginalText;

        return ContainsSummaryKeyword(description) || ContainsSummaryKeyword(originalText);
    }

    private static bool ContainsSummaryKeyword(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var normalized = text.Trim();
        foreach (var keyword in SummaryKeywords)
        {
            if (normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static StatementUploadDto MapToDto(StatementUpload upload) => new(
        upload.Id,
        upload.OriginalFileName,
        upload.UploadedAt,
        upload.Status,
        upload.ErrorMessage,
        upload.ParsedTransactions.Select(t => new ParsedTransactionDto(
            t.Id,
            t.Date,
            t.Description,
            t.Amount,
            t.Currency,
            t.SuggestedCategoryId,
            t.ConfirmedCategoryId,
            t.EffectiveCategoryId,
            t.OriginalText
        )).ToList()
    );
}
