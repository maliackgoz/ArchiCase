using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public class DebtInquiryClient : IDebtInquiryClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DebtInquiryClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public DebtInquiryClient(HttpClient httpClient, ILogger<DebtInquiryClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DebtInquiryResponse> GetDebtAsync(int subscriptionId)
    {
        var url = $"api/external/debt-inquiry/{subscriptionId}";
        var sw = Stopwatch.StartNew();

        var response = await _httpClient.GetAsync(url);
        sw.Stop();

        _logger.LogInformation("HTTP GET {Url} responded {StatusCode} in {Ms}ms",
            url, (int)response.StatusCode, sw.ElapsedMilliseconds);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<DebtInquiryResponse>(content, JsonOptions)!;
    }
}
