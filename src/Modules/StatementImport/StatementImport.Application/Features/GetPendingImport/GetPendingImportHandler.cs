using SpendBear.SharedKernel;
using StatementImport.Application.DTOs;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.GetPendingImport;

public sealed class GetPendingImportHandler
{
    private readonly IStatementUploadRepository _repository;

    public GetPendingImportHandler(IStatementUploadRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<StatementUploadDto>> Handle(
        Guid uploadId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var upload = await _repository.GetByIdWithTransactionsAsync(uploadId, cancellationToken);

        if (upload == null)
            return Result.Failure<StatementUploadDto>(StatementImportErrors.UploadNotFound);

        if (upload.UserId != userId)
            return Result.Failure<StatementUploadDto>(StatementImportErrors.NotAuthorized);

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
