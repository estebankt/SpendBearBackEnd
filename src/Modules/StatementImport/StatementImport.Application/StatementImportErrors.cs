using SpendBear.SharedKernel;

namespace StatementImport.Application;

public static class StatementImportErrors
{
    public static readonly Error UploadNotFound = new("StatementImport.NotFound", "Statement upload not found.");
    public static readonly Error NotAuthorized = new("StatementImport.NotAuthorized", "You are not authorized to access this import.");
    public static readonly Error InvalidStatus = new("StatementImport.InvalidStatus", "This operation is not allowed for the current import status.");
    public static readonly Error PdfExtractionFailed = new("StatementImport.PdfExtractionFailed", "Failed to extract text from the PDF.");
    public static readonly Error AiParsingFailed = new("StatementImport.AiParsingFailed", "Failed to parse the statement using AI.");
    public static readonly Error NoTransactionsParsed = new("StatementImport.NoTransactions", "No transactions were found in the statement.");
    public static readonly Error FileStorageFailed = new("StatementImport.FileStorageFailed", "Failed to store the uploaded file.");
}
