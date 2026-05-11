namespace SubscriptionApp.Infrastructure.ExternalServices.Models;

public class NotificationRequest
{
    public string Channel { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
