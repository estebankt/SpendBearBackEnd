using SpendBear.SharedKernel;

namespace StatementImport.Application.Abstractions;

public interface IPdfTextExtractor
{
    Task<Result<string>> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default);
}
