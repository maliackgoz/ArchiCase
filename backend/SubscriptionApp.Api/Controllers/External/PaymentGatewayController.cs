using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Api.Controllers.External;

[ApiController]
[Route("api/external/payment-gateway")]
public class PaymentGatewayController : ControllerBase
{
    private static readonly string[] ErrorCodes = ["INSUFFICIENT_FUNDS", "GATEWAY_TIMEOUT", "DECLINED"];

    [HttpPost]
    public ActionResult<PaymentGatewayResponse> Process([FromBody] PaymentGatewayRequest request)
    {
        var rng = new Random();

        // ~10% failure rate
        if (rng.NextDouble() < 0.10)
        {
            var errorCode = ErrorCodes[rng.Next(ErrorCodes.Length)];
            return BadRequest(new PaymentGatewayResponse
            {
                Success = false,
                ErrorCode = errorCode
            });
        }

        return Ok(new PaymentGatewayResponse
        {
            Success = true,
            TransactionId = $"TXN-{Guid.NewGuid()}"
        });
    }
}
