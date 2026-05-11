using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Infrastructure.ExternalServices.Models;

namespace SubscriptionApp.Api.Controllers.External;

[ApiController]
[Route("api/external/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(ILogger<NotificationController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Send([FromBody] NotificationRequest request)
    {
        _logger.LogInformation(
            "Notification sent — channel: {Channel}, recipient: {Recipient}, message: {Message}",
            request.Channel, request.Recipient, request.Message);

        return Ok();
    }
}
