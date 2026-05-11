namespace SubscriptionApp.Domain.Exceptions;

// NotFoundException maps to HTTP 404, NOT 409 — it is NOT a DomainException subclass.
public class NotFoundException : Exception
{
    public string Code { get; }

    public NotFoundException(string entityName, int id)
        : base($"{entityName} with id {id} was not found.")
    {
        Code = "NOT_FOUND";
    }
}
