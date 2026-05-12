using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Subscriptions;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
[Authorize(Roles = "Admin")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SubscriptionResponse>>> GetAll([FromQuery] int? customerId)
    {
        var subscriptions = await _subscriptionService.GetAllAsync(customerId);
        return Ok(subscriptions.Select(s => s.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubscriptionResponse>> GetById(int id)
    {
        var subscription = await _subscriptionService.GetByIdAsync(id);
        return Ok(subscription.ToResponse());
    }
}
