using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Payments;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentResponse>>> GetAll([FromQuery] int? subscriptionId)
    {
        var payments = await _paymentService.GetAllAsync(subscriptionId);
        return Ok(payments.Select(p => p.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PaymentResponse>> GetById(int id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        return Ok(payment.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> Create([FromBody] CreatePaymentRequest request)
    {
        var payment = await _paymentService.CreateAsync(
            request.SubscriptionId, request.Amount, request.Period);
        var response = payment.ToResponse();
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }
}
