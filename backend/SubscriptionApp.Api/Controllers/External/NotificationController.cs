using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Infrastructure.ExternalServices.Models;
using SubscriptionApp.Infrastructure.Persistence;
using SubscriptionApp.Infrastructure.Utilities;

namespace SubscriptionApp.Api.Controllers.External;

[ApiController]
[Route("api/external/notifications")]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> _logger;
    private readonly AppDbContext _db;

    public NotificationController(ILogger<NotificationController> logger, AppDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] NotificationRequest request)
    {
        _logger.LogInformation(
            "Notification sent — channel: {Channel}, recipient: {Recipient}, message: {Message}",
            request.Channel, request.Recipient, request.Message);

        _db.Notifications.Add(new Notification
        {
            Channel = request.Channel,
            Recipient = request.Recipient,
            Message = request.Message,
            SentAt = BusinessClock.Now()
        });
        await _db.SaveChangesAsync();

        return Ok();
    }
}
