using SpendBear.SharedKernel;
using StatementImport.Application.DTOs;
using StatementImport.Domain.Repositories;

namespace StatementImport.Application.Features.GetUserImports;

public sealed class GetUserImportsHandler
{
    private readonly IStatementUploadRepository _repository;

    public GetUserImportsHandler(IStatementUploadRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<StatementUploadSummaryDto>>> Handle(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var uploads = await _repository.GetByUserIdAsync(userId, cancellationToken);

        var dtos = uploads.Select(u => new StatementUploadSummaryDto(
            u.Id,
            u.OriginalFileName,
            u.UploadedAt,
            u.Status,
            u.ParsedTransactions.Count
        )).ToList();

        return Result.Success(dtos);
    }
}
