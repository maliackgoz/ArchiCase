using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.Persistence;

namespace SubscriptionApp.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly AppDbContext _db;

    public CustomerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        return await _db.Customers
            .Include(c => c.Subscriptions)
            .ToListAsync();
    }

    public async Task<Customer> GetByIdAsync(int id)
    {
        var customer = await _db.Customers
            .Include(c => c.Subscriptions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer is null)
            throw new NotFoundException(nameof(Customer), id);

        return customer;
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        var emailExists = await _db.Customers
            .AnyAsync(c => c.Email == customer.Email);

        if (emailExists)
            throw new DomainException("DUPLICATE_EMAIL",
                $"A customer with email '{customer.Email}' already exists.");

        customer.CreatedAt = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return customer;
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);

        if (customer is null)
            throw new NotFoundException(nameof(Customer), id);

        _db.Customers.Remove(customer);
        await _db.SaveChangesAsync();
    }

    public async Task<CustomerDashboardData> GetDashboardAsync(int customerId)
    {
        var customer = await _db.Customers.FindAsync(customerId);
        if (customer is null)
            throw new NotFoundException(nameof(Customer), customerId);

        var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearEnd = yearStart.AddYears(1);

        var subscriptions = await _db.Subscriptions
            .Where(s => s.CustomerId == customerId)
            .ToListAsync();

        var activeIds = subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Select(s => s.Id)
            .ToList();

        // Subscription IDs that already have a Successful payment for the current month
        var paidThisMonthIds = await _db.Payments
            .Where(p => activeIds.Contains(p.SubscriptionId) &&
                        p.Period == currentPeriod &&
                        p.Status == PaymentStatus.Successful)
            .Select(p => p.SubscriptionId)
            .ToListAsync();

        var unpaid = subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && !paidThisMonthIds.Contains(s.Id))
            .Select(s => (s.Id, s.ProviderName, s.SubscriptionType))
            .ToList();

        var allIds = subscriptions.Select(s => s.Id).ToList();

        var recentPayments = await _db.Payments
            .Where(p => allIds.Contains(p.SubscriptionId))
            .OrderByDescending(p => p.PaymentDate)
            .Take(10)
            .ToListAsync();

        var totalPaidThisYear = await _db.Payments
            .Where(p => allIds.Contains(p.SubscriptionId) &&
                        p.Status == PaymentStatus.Successful &&
                        p.PaymentDate >= yearStart && p.PaymentDate < yearEnd)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;

        return new CustomerDashboardData
        {
            ActiveSubscriptionCount = activeIds.Count,
            UnpaidThisMonth = unpaid,
            RecentPayments = recentPayments,
            TotalPaidThisYear = totalPaidThisYear
        };
    }
}
