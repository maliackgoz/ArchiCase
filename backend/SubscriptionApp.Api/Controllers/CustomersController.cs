using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Customers;
using SubscriptionApp.Api.Dtos.Payments;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Infrastructure.Services;

// Customer creation is owned by AuthService.RegisterAsync (the public signup flow).
// Admins monitor + off-board only — no POST endpoint here.

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerResponse>>> GetAll()
    {
        var customers = await _customerService.GetAllAsync();
        return Ok(customers.Select(c => c.ToResponse()).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        return Ok(customer.ToResponse());
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/dashboard")]
    public async Task<ActionResult<CustomerDashboardResponse>> GetDashboard(int id)
    {
        var data = await _customerService.GetDashboardAsync(id);

        var response = new CustomerDashboardResponse
        {
            ActiveSubscriptionCount = data.ActiveSubscriptionCount,
            UnpaidThisMonth = data.UnpaidThisMonth
                .Select(s => new UnpaidSubscriptionSummary
                {
                    Id = s.Id,
                    ProviderName = s.ProviderName,
                    SubscriptionType = s.SubscriptionType
                })
                .ToList(),
            RecentPayments = data.RecentPayments.Select(p => new DashboardPaymentResponse
            {
                Id = p.Id,
                SubscriptionId = p.SubscriptionId,
                ProviderName = p.ProviderName,
                SubscriptionType = p.SubscriptionType,
                Amount = p.Amount,
                Period = p.Period,
                Status = p.Status,
                PaymentDate = p.PaymentDate,
                ExternalTransactionId = p.ExternalTransactionId
            }).ToList(),
            TotalPaidThisYear = data.TotalPaidThisYear
        };

        return Ok(response);
    }
}
