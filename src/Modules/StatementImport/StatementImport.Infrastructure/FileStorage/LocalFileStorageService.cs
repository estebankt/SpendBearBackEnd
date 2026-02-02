using Microsoft.Extensions.Configuration;
using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;

namespace StatementImport.Infrastructure.FileStorage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? "./uploads/statements";
    }

    public async Task<Result<string>> SaveFileAsync(Stream fileStream, string fileName, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userDir = Path.Combine(_basePath, userId.ToString());
            Directory.CreateDirectory(userDir);

            var storedFileName = $"{Guid.NewGuid()}.pdf";
            var filePath = Path.Combine(userDir, storedFileName);

            using var fs = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fs, cancellationToken);

            return Result.Success(filePath);
        }
        catch (Exception ex)
        {
            return Result.Failure<string>(
                new Error("FileStorage.SaveFailed", $"Failed to save file: {ex.Message}"));
        }
    }

    public Task<Result<Stream>> GetFileAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(storedPath))
                return Task.FromResult(Result.Failure<Stream>(
                    new Error("FileStorage.NotFound", "File not found.")));

            Stream stream = new FileStream(storedPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(Result.Success(stream));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<Stream>(
                new Error("FileStorage.ReadFailed", $"Failed to read file: {ex.Message}")));
        }
    }

    public Task<Result> DeleteFileAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(storedPath))
                File.Delete(storedPath);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(
                new Error("FileStorage.DeleteFailed", $"Failed to delete file: {ex.Message}")));
        }
    }
}
