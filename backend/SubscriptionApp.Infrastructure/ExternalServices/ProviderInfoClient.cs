using System.Diagnostics;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public class ProviderInfoClient : IProviderInfoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProviderInfoClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public ProviderInfoClient(HttpClient httpClient, ILogger<ProviderInfoClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProviderInfoResponse> GetProviderInfoAsync(string providerName, string subscriptionNumber)
    {
        var url = $"api/external/provider-info?providerName={HttpUtility.UrlEncode(providerName)}&subscriptionNumber={HttpUtility.UrlEncode(subscriptionNumber)}";
        var sw = Stopwatch.StartNew();

        var response = await _httpClient.GetAsync(url);
        sw.Stop();

        _logger.LogInformation("HTTP GET {Url} responded {StatusCode} in {Ms}ms",
            url, (int)response.StatusCode, sw.ElapsedMilliseconds);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProviderInfoResponse>(content, JsonOptions)!;
    }
}
