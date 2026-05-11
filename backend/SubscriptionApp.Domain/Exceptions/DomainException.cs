namespace SubscriptionApp.Domain.Exceptions;

/// <summary>
/// Base for all domain rule violations. Maps to HTTP 409 in the exception middleware.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
