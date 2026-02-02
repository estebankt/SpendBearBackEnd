using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.ConfirmImport;

public sealed class ConfirmImportHandler
{
    private readonly IStatementUploadRepository _repository;
    private readonly IStatementImportUnitOfWork _unitOfWork;
    private readonly ITransactionCreationService _transactionCreationService;

    public ConfirmImportHandler(
        IStatementUploadRepository repository,
        IStatementImportUnitOfWork unitOfWork,
        ITransactionCreationService transactionCreationService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _transactionCreationService = transactionCreationService;
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

        // Create transactions in Spending module
        foreach (var pt in upload.ParsedTransactions)
        {
            var createResult = await _transactionCreationService.CreateTransactionAsync(
                userId,
                pt.Amount,
                pt.Currency,
                pt.Date,
                pt.Description,
                pt.EffectiveCategoryId,
                cancellationToken);

            if (createResult.IsFailure)
                return Result.Failure(StatementImportErrors.TransactionCreationFailed);
        }

        await _repository.UpdateAsync(upload, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
