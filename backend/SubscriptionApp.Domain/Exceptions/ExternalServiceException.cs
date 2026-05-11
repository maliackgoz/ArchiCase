namespace SubscriptionApp.Domain.Exceptions;

/// <summary>
/// Thrown when the payment gateway declines or errors. Maps to HTTP 502 in middleware.
/// NOT a DomainException subclass — this is an infrastructure failure, not a business rule violation.
/// </summary>
public class ExternalServiceException : Exception
{
    public string Code { get; }

    public ExternalServiceException(string errorCode)
        : base($"Payment gateway returned an error: {errorCode}.")
    {
        Code = errorCode;
    }
}
