namespace SubscriptionApp.Api.Dtos.Notifications;

public class NotificationResponse
{
    public int Id { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    // Resolved from recipient (phone for SMS, email for EMAIL) — null if no match.
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}
