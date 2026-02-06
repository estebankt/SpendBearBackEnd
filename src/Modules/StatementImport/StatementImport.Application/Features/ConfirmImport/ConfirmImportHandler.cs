using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.ConfirmImport;

public sealed class ConfirmImportHandler
{
    private readonly IStatementUploadRepository _repository;
    private readonly IStatementImportUnitOfWork _unitOfWork;

    public ConfirmImportHandler(
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

        var confirmResult = upload.Confirm();
        if (confirmResult.IsFailure)
            return confirmResult;

        // Transaction creation is handled by StatementImportConfirmedEvent
        // dispatched during SaveChangesAsync via the Spending module's event handler

        await _repository.UpdateAsync(upload, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
