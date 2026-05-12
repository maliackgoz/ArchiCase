using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Payments;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ISubscriptionService _subscriptionService;

    public PaymentsController(
        IPaymentService paymentService,
        ISubscriptionService subscriptionService)
    {
        _paymentService = paymentService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<PaymentResponse>>> GetAll([FromQuery] int? subscriptionId)
    {
        var payments = await _paymentService.GetAllAsync(subscriptionId);
        return Ok(payments.Select(p => p.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PaymentResponse>> GetById(int id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        return Ok(payment.ToResponse());
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request)
    {
        var customerIdClaim = User.FindFirstValue("customerId");
        if (customerIdClaim is null) return Forbid();
        var currentCustomerId = int.Parse(customerIdClaim);

        var subscription = await _subscriptionService.GetByIdAsync(request.SubscriptionId);
        if (subscription.CustomerId != currentCustomerId) return Forbid();

        var payment = await _paymentService.CreateAsync(
            request.SubscriptionId, request.Amount, request.Period);
        var response = payment.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
