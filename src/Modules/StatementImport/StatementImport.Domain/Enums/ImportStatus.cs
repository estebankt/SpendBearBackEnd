namespace StatementImport.Domain.Enums;

public enum ImportStatus
{
    Uploading,
    Parsing,
    PendingReview,
    Confirmed,
    Failed,
    Cancelled
}
