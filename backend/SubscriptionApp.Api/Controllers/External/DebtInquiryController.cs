using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Infrastructure.ExternalServices.Models;
using SubscriptionApp.Infrastructure.Persistence;

namespace SubscriptionApp.Api.Controllers.External;

[ApiController]
[Route("api/external/debt-inquiry")]
public class DebtInquiryController : ControllerBase
{
    private readonly AppDbContext _db;

    public DebtInquiryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{subscriptionId:int}")]
    public async Task<ActionResult<DebtInquiryResponse>> Get(int subscriptionId)
    {
        var subscription = await _db.Subscriptions.FindAsync(subscriptionId);
        if (subscription is null)
            return NotFound();

        // Seeded random: same subscriptionId always returns same amount (deterministic mock).
        var rng = new Random(subscriptionId);
        var amount = subscription.SubscriptionType switch
        {
            SubscriptionType.Electricity => rng.Next(100, 401),
            SubscriptionType.Water       => rng.Next(50, 151),
            SubscriptionType.Internet    => rng.Next(150, 251),
            SubscriptionType.Gsm         => rng.Next(80, 201),
            SubscriptionType.NaturalGas  => rng.Next(120, 351),
            _                            => rng.Next(50, 201)
        };

        return Ok(new DebtInquiryResponse
        {
            SubscriptionId = subscriptionId,
            Amount = amount,
            Period = DateTime.UtcNow.ToString("yyyy-MM"),
            Currency = "TRY"
        });
    }
}
