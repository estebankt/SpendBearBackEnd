using System.Text;
using SpendBear.SharedKernel;
using StatementImport.Application.Abstractions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace StatementImport.Infrastructure.Services;

public class PdfTextExtractor : IPdfTextExtractor
{
    public Task<Result<string>> ExtractTextAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        try
        {
            using var document = PdfDocument.Open(pdfStream);
            var sb = new StringBuilder();

            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }

            var text = sb.ToString();

            if (string.IsNullOrWhiteSpace(text))
                return Task.FromResult(Result.Failure<string>(
                    new Error("PdfExtraction.EmptyContent", "No text could be extracted from the PDF.")));

            return Task.FromResult(Result.Success(text));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<string>(
                new Error("PdfExtraction.Error", $"Failed to extract text from PDF: {ex.Message}")));
        }
    }
}
