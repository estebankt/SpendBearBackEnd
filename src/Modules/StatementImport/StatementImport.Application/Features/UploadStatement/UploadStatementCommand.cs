namespace StatementImport.Application.Features.UploadStatement;

public sealed record UploadStatementCommand(Stream FileStream, string FileName);
