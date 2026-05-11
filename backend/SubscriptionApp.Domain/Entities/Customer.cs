namespace SubscriptionApp.Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Non-virtual: lazy loading is disabled; use explicit .Include() in queries.
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
