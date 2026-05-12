using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.Persistence;

namespace SubscriptionApp.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _db;

    public SubscriptionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Subscription>> GetAllAsync(int? customerId)
    {
        var query = _db.Subscriptions
            .Include(s => s.Customer)
            .AsQueryable();

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        return await query.ToListAsync();
    }

    public async Task<Subscription> GetByIdAsync(int id)
    {
        var subscription = await _db.Subscriptions
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription is null)
            throw new NotFoundException(nameof(Subscription), id);

        return subscription;
    }

    public async Task<Subscription> CreateAsync(Subscription subscription)
    {
        var customerExists = await _db.Customers.AnyAsync(c => c.Id == subscription.CustomerId);
        if (!customerExists)
            throw new NotFoundException(nameof(Customer), subscription.CustomerId);

        var duplicateSubscription = await _db.Subscriptions.AnyAsync(s =>
            s.ProviderName == subscription.ProviderName &&
            s.SubscriptionNumber == subscription.SubscriptionNumber);

        if (duplicateSubscription)
            throw new DomainException("DUPLICATE_SUBSCRIPTION",
                $"A subscription with provider '{subscription.ProviderName}' and number '{subscription.SubscriptionNumber}' already exists.");

        subscription.Status = SubscriptionStatus.Active;
        // On create, the customer's chosen payment day defaults to the provider's last payment day
        // (the latest acceptable date in the [billing, lastPayment] window).
        if (subscription.PaymentDayOfMonth == 0)
            subscription.PaymentDayOfMonth = subscription.LastPaymentDayOfMonth;
        subscription.CreatedAt = DateTime.UtcNow;
        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(subscription.Id);
    }

    public async Task<Subscription> UpdateAsync(int id, SubscriptionStatus status, string providerName, int paymentDayOfMonth, bool isAutoPay)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription is null)
            throw new NotFoundException(nameof(Subscription), id);

        // Check compound uniqueness only when ProviderName changes
        if (subscription.ProviderName != providerName)
        {
            var duplicate = await _db.Subscriptions.AnyAsync(s =>
                s.Id != id &&
                s.ProviderName == providerName &&
                s.SubscriptionNumber == subscription.SubscriptionNumber);

            if (duplicate)
                throw new DomainException("DUPLICATE_SUBSCRIPTION",
                    $"A subscription with provider '{providerName}' and number '{subscription.SubscriptionNumber}' already exists.");
        }

        // Payment day must sit inside the provider's [billing, lastPayment] window.
        if (paymentDayOfMonth < subscription.BillingDayOfMonth ||
            paymentDayOfMonth > subscription.LastPaymentDayOfMonth)
        {
            throw new DomainException("PAYMENT_DAY_OUT_OF_RANGE",
                $"Payment day must be between {subscription.BillingDayOfMonth} (billing day) and {subscription.LastPaymentDayOfMonth} (last payment day).");
        }

        // BillingDayOfMonth and LastPaymentDayOfMonth are provider-owned and cannot be modified here.
        subscription.Status = status;
        subscription.ProviderName = providerName;
        subscription.PaymentDayOfMonth = paymentDayOfMonth;
        subscription.IsAutoPay = isAutoPay;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var subscription = await _db.Subscriptions.FindAsync(id);
        if (subscription is null)
            throw new NotFoundException(nameof(Subscription), id);

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
    }
}
