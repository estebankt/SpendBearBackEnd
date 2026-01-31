namespace SpendBear.SharedKernel;

/// <summary>
/// Exception thrown when a domain invariant is violated.
/// Use this sparingly - prefer Result pattern for expected failures.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
