using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SubscriptionApp.Api.Dtos.Customers;
using SubscriptionApp.Api.Dtos.Payments;
using SubscriptionApp.Api.Dtos.Subscriptions;
using SubscriptionApp.Api.Dtos.Users;
using SubscriptionApp.Api.Mapping;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.ExternalServices;
using SubscriptionApp.Infrastructure.ExternalServices.Models;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Customer")]
public class UsersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IUserService _userService;
    private readonly IPaymentService _paymentService;
    private readonly IProviderInfoClient _providerInfoClient;

    public UsersController(
        ICustomerService customerService,
        ISubscriptionService subscriptionService,
        IUserService userService,
        IPaymentService paymentService,
        IProviderInfoClient providerInfoClient)
    {
        _customerService = customerService;
        _subscriptionService = subscriptionService;
        _userService = userService;
        _paymentService = paymentService;
        _providerInfoClient = providerInfoClient;
    }

    private int CurrentCustomerId =>
        int.Parse(User.FindFirstValue("customerId")!);

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var customer = await _customerService.GetByIdAsync(CurrentCustomerId);
        return Ok(customer.ToResponse());
    }

    [HttpGet("me/subscriptions")]
    public async Task<IActionResult> GetMySubscriptions()
    {
        var subscriptions = await _subscriptionService.GetAllAsync(CurrentCustomerId);
        return Ok(subscriptions.Select(s => s.ToResponse()).ToList());
    }

    [HttpGet("me/subscriptions/{id:int}")]
    public async Task<IActionResult> GetMySubscription(int id)
    {
        var sub = await _subscriptionService.GetByIdAsync(id);
        if (sub.CustomerId != CurrentCustomerId) return Forbid();
        return Ok(sub.ToResponse());
    }

    [HttpPost("me/subscriptions")]
    public async Task<IActionResult> CreateMySubscription([FromBody] CreateMySubscriptionRequest request)
    {
        // Billing day is fetched from the provider service — customers don't enter it.
        ProviderInfoResponse providerInfo;
        try
        {
            providerInfo = await _providerInfoClient.GetProviderInfoAsync(
                request.ProviderName, request.SubscriptionNumber);
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException("PROVIDER_INFO_FAILED",
                $"Could not retrieve billing day from provider '{request.ProviderName}'.", ex);
        }

        var sub = await _subscriptionService.CreateAsync(new Subscription
        {
            CustomerId = CurrentCustomerId,
            SubscriptionType = request.SubscriptionType,
            ProviderName = request.ProviderName,
            SubscriptionNumber = request.SubscriptionNumber,
            BillingDayOfMonth = providerInfo.BillingDayOfMonth,
            LastPaymentDayOfMonth = providerInfo.LastPaymentDayOfMonth,
            IsAutoPay = false
        });

        var response = sub.ToResponse();
        return CreatedAtAction(nameof(GetMySubscription), new { id = response.Id }, response);
    }

    [HttpPut("me/subscriptions/{id:int}")]
    public async Task<IActionResult> UpdateMySubscription(int id, [FromBody] UpdateSubscriptionRequest request)
    {
        var existing = await _subscriptionService.GetByIdAsync(id);
        if (existing.CustomerId != CurrentCustomerId) return Forbid();

        var updated = await _subscriptionService.UpdateAsync(
            id, request.Status, request.ProviderName, request.PaymentDayOfMonth, request.IsAutoPay);
        return Ok(updated.ToResponse());
    }

    [HttpDelete("me/subscriptions/{id:int}")]
    public async Task<IActionResult> DeleteMySubscription(int id)
    {
        var existing = await _subscriptionService.GetByIdAsync(id);
        if (existing.CustomerId != CurrentCustomerId) return Forbid();

        await _subscriptionService.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("me/subscriptions/{id:int}/payments")]
    public async Task<IActionResult> GetMySubscriptionPayments(int id)
    {
        var subscription = await _subscriptionService.GetByIdAsync(id);
        if (subscription.CustomerId != CurrentCustomerId) return Forbid();

        var payments = await _paymentService.GetAllAsync(id);
        return Ok(payments
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => p.ToResponse())
            .ToList());
    }

    [HttpGet("me/dashboard")]
    public async Task<IActionResult> GetMyDashboard()
    {
        var data = await _customerService.GetDashboardAsync(CurrentCustomerId);
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

    [HttpGet("me/reminders")]
    public async Task<IActionResult> GetMyReminders()
    {
        var reminders = await _userService.GetRemindersAsync(CurrentCustomerId);
        return Ok(reminders.Select(r => new ReminderResponse
        {
            SubscriptionId = r.SubscriptionId,
            ProviderName = r.ProviderName,
            SubscriptionType = (int)r.SubscriptionType,
            BillingDayOfMonth = r.BillingDayOfMonth,
            LastPaymentDayOfMonth = r.LastPaymentDayOfMonth,
            PaymentDayOfMonth = r.PaymentDayOfMonth,
            IsAutoPay = r.IsAutoPay,
            DaysUntilDue = r.DaysUntilDue,
            Period = r.Period,
            IsOverdue = r.IsOverdue
        }));
    }

    [HttpPost("me/subscriptions/process-auto-pay")]
    public async Task<IActionResult> ProcessAutoPay()
    {
        var result = await _userService.ProcessAutoPayAsync(CurrentCustomerId);
        return Ok(result);
    }
}
