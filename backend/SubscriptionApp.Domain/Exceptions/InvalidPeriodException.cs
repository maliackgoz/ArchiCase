namespace SubscriptionApp.Domain.Exceptions;

public class InvalidPeriodException : DomainException
{
    public string Period { get; }

    public InvalidPeriodException(string period)
        : base("INVALID_PERIOD",
               $"'{period}' is not a valid period. Expected format: YYYY-MM (e.g. 2026-05).")
    {
        Period = period;
    }
}
