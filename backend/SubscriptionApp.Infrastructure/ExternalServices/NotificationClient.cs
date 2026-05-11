using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(HttpClient httpClient, ILogger<NotificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task SendAsync(string channel, string recipient, string message)
    {
        const string url = "api/external/notifications";
        var sw = Stopwatch.StartNew();

        var response = await _httpClient.PostAsJsonAsync(url,
            new NotificationRequest { Channel = channel, Recipient = recipient, Message = message });
        sw.Stop();

        _logger.LogInformation("HTTP POST {Url} responded {StatusCode} in {Ms}ms",
            url, (int)response.StatusCode, sw.ElapsedMilliseconds);

        response.EnsureSuccessStatusCode();
    }
}
