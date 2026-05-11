using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;

namespace SubscriptionApp.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<AppDbContext>();

        // Idempotent: skip seeding if customers already exist.
        if (await context.Customers.AnyAsync())
            return;

        var customers = new List<Customer>
        {
            new()
            {
                FullName = "Ahmet Yılmaz",
                Email = "ahmet.yilmaz@example.com",
                PhoneNumber = "+905551234567",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                FullName = "Fatma Kaya",
                Email = "fatma.kaya@example.com",
                PhoneNumber = "+905559876543",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                FullName = "Mehmet Demir",
                Email = "mehmet.demir@example.com",
                PhoneNumber = "+905553456789",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        var subscriptions = new List<Subscription>
        {
            new()
            {
                CustomerId = customers[0].Id,
                SubscriptionType = SubscriptionType.Electricity,
                ProviderName = "BEDAŞ",                 // Boğaziçi Elektrik — Istanbul distribution
                SubscriptionNumber = "BEDAS-001",
                Status = SubscriptionStatus.Active,
                BillingDayOfMonth = 5,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                CustomerId = customers[0].Id,
                SubscriptionType = SubscriptionType.Internet,
                ProviderName = "Türk Telekom",
                SubscriptionNumber = "TT-NET-002",
                Status = SubscriptionStatus.Active,
                BillingDayOfMonth = 15,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                CustomerId = customers[1].Id,
                SubscriptionType = SubscriptionType.Water,
                ProviderName = "İSKİ",                  // Istanbul water utility
                SubscriptionNumber = "ISKI-003",
                Status = SubscriptionStatus.Active,
                BillingDayOfMonth = 10,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                CustomerId = customers[1].Id,
                SubscriptionType = SubscriptionType.Gsm,
                ProviderName = "Türkcell",
                SubscriptionNumber = "TC-004",
                Status = SubscriptionStatus.Active,
                BillingDayOfMonth = 20,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                CustomerId = customers[2].Id,
                SubscriptionType = SubscriptionType.NaturalGas,
                ProviderName = "İGDAŞ",                 // Istanbul gas distribution
                SubscriptionNumber = "IGDAS-005",
                Status = SubscriptionStatus.Active,
                BillingDayOfMonth = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Subscriptions.AddRange(subscriptions);
        await context.SaveChangesAsync();
    }
}
