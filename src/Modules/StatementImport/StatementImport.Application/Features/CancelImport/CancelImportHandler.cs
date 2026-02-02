using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.CancelImport;

public sealed class CancelImportHandler
{
    private readonly IStatementUploadRepository _repository;
    private readonly IStatementImportUnitOfWork _unitOfWork;

    public CancelImportHandler(
        IStatementUploadRepository repository,
        IStatementImportUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        Guid uploadId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var upload = await _repository.GetByIdWithTransactionsAsync(uploadId, cancellationToken);

        if (upload == null)
            return Result.Failure(StatementImportErrors.UploadNotFound);

        if (upload.UserId != userId)
            return Result.Failure(StatementImportErrors.NotAuthorized);

        var cancelResult = upload.Cancel();
        if (cancelResult.IsFailure)
            return cancelResult;

        await _repository.UpdateAsync(upload, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
