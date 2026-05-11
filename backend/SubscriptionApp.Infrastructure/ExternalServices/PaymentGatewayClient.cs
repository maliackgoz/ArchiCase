using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Infrastructure.ExternalServices;

public class PaymentGatewayClient : IPaymentGatewayClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentGatewayClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public PaymentGatewayClient(HttpClient httpClient, ILogger<PaymentGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentGatewayResponse> ProcessPaymentAsync(int subscriptionId, decimal amount)
    {
        const string url = "api/external/payment-gateway";
        var sw = Stopwatch.StartNew();

        var httpResponse = await _httpClient.PostAsJsonAsync(url,
            new PaymentGatewayRequest { SubscriptionId = subscriptionId, Amount = amount });
        sw.Stop();

        _logger.LogInformation("HTTP POST {Url} responded {StatusCode} in {Ms}ms",
            url, (int)httpResponse.StatusCode, sw.ElapsedMilliseconds);

        // Both 200 (success) and 400 (gateway decline) return a PaymentGatewayResponse body.
        var content = await httpResponse.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaymentGatewayResponse>(content, JsonOptions)!;
    }
}
