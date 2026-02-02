using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Application.DTOs;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.UpdateParsedTransactions;

public sealed class UpdateParsedTransactionsHandler
{
    private readonly IStatementUploadRepository _repository;
    private readonly IStatementImportUnitOfWork _unitOfWork;

    public UpdateParsedTransactionsHandler(
        IStatementUploadRepository repository,
        IStatementImportUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StatementUploadDto>> Handle(
        UpdateParsedTransactionsCommand command,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var upload = await _repository.GetByIdWithTransactionsAsync(command.UploadId, cancellationToken);

        if (upload == null)
            return Result.Failure<StatementUploadDto>(StatementImportErrors.UploadNotFound);

        if (upload.UserId != userId)
            return Result.Failure<StatementUploadDto>(StatementImportErrors.NotAuthorized);

        foreach (var update in command.Updates)
        {
            var result = upload.UpdateTransactionCategory(update.ParsedTransactionId, update.NewCategoryId);
            if (result.IsFailure)
                return Result.Failure<StatementUploadDto>(result.Error);
        }

        await _repository.UpdateAsync(upload, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new StatementUploadDto(
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
        ));
    }
}
