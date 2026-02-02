using SpendBear.SharedKernel;

namespace StatementImport.Application.Abstractions;

public interface IFileStorageService
{
    Task<Result<string>> SaveFileAsync(Stream fileStream, string fileName, Guid userId, CancellationToken cancellationToken = default);
    Task<Result<Stream>> GetFileAsync(string storedPath, CancellationToken cancellationToken = default);
    Task<Result> DeleteFileAsync(string storedPath, CancellationToken cancellationToken = default);
}
