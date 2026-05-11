namespace SubscriptionApp.Infrastructure.ExternalServices;

public interface INotificationClient
{
    Task SendAsync(string channel, string recipient, string message);
}
