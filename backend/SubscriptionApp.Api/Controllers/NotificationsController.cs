using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Api.Dtos.Notifications;
using SubscriptionApp.Infrastructure.Persistence;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Admin")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> GetAll(
        [FromQuery] string? channel = null,
        [FromQuery] int take = 100)
    {
        var query = _db.Notifications.AsQueryable();
        if (!string.IsNullOrWhiteSpace(channel))
            query = query.Where(n => n.Channel == channel.ToUpper());

        var notifications = await query
            .OrderByDescending(n => n.SentAt)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync();

        // Resolve recipient → Customer by matching phone (SMS) or email (EMAIL).
        var recipients = notifications.Select(n => n.Recipient).Distinct().ToList();
        var matchingCustomers = await _db.Customers
            .Where(c => recipients.Contains(c.PhoneNumber) || recipients.Contains(c.Email))
            .Select(c => new { c.Id, c.FullName, c.PhoneNumber, c.Email })
            .ToListAsync();

        var byPhone = matchingCustomers.ToDictionary(c => c.PhoneNumber, c => c);
        var byEmail = matchingCustomers.ToDictionary(c => c.Email, c => c);

        var rows = notifications.Select(n =>
        {
            var match = n.Channel == "SMS"
                ? (byPhone.TryGetValue(n.Recipient, out var phoneMatch) ? phoneMatch : null)
                : (byEmail.TryGetValue(n.Recipient, out var emailMatch) ? emailMatch : null);

            return new NotificationResponse
            {
                Id = n.Id,
                Channel = n.Channel,
                Recipient = n.Recipient,
                Message = n.Message,
                SentAt = n.SentAt,
                CustomerId = match?.Id,
                CustomerName = match?.FullName
            };
        }).ToList();

        return Ok(rows);
    }
}
