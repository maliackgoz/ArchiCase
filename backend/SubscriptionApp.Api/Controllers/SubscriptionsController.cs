using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Subscriptions;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
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

    [HttpPost]
    public async Task<ActionResult<SubscriptionResponse>> Create([FromBody] CreateSubscriptionRequest request)
    {
        var subscription = await _subscriptionService.CreateAsync(request.ToEntity());
        var response = subscription.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SubscriptionResponse>> Update(int id, [FromBody] UpdateSubscriptionRequest request)
    {
        var subscription = await _subscriptionService.UpdateAsync(
            id, request.Status, request.ProviderName, request.BillingDayOfMonth);
        return Ok(subscription.ToResponse());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _subscriptionService.DeleteAsync(id);
        return NoContent();
    }
}
