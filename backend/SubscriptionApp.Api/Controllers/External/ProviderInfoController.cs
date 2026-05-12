using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Api.Controllers.External;

[ApiController]
[Route("api/external/provider-info")]
public class ProviderInfoController : ControllerBase
{
    // Customer must pay within 7 days of the billing day.
    private const int PaymentWindowDays = 7;

    [HttpGet]
    public ActionResult<ProviderInfoResponse> Get(
        [FromQuery] string providerName,
        [FromQuery] string subscriptionNumber)
    {
        if (string.IsNullOrWhiteSpace(providerName) ||
            string.IsNullOrWhiteSpace(subscriptionNumber))
        {
            return BadRequest(new
            {
                error = new
                {
                    code = "MISSING_PARAMETERS",
                    message = "providerName and subscriptionNumber are required."
                }
            });
        }

        // Deterministic mock: same (provider, number) always returns the same billing day.
        // Cap billing day at 21 so lastPaymentDay = billing + 7 stays within [8, 28].
        var key = $"{providerName}|{subscriptionNumber}";
        var hash = 0;
        foreach (var ch in key) hash = unchecked(hash * 31 + ch);
        var billingDay = (Math.Abs(hash) % 21) + 1;
        var lastPaymentDay = billingDay + PaymentWindowDays;

        return Ok(new ProviderInfoResponse
        {
            ProviderName = providerName,
            SubscriptionNumber = subscriptionNumber,
            BillingDayOfMonth = billingDay,
            LastPaymentDayOfMonth = lastPaymentDay
        });
    }
}
