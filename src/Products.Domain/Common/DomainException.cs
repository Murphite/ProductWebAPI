namespace Products.Domain.Common;

/// <summary>
/// Thrown when a domain invariant is violated. Mapped to HTTP 400 by the API.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException()
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
