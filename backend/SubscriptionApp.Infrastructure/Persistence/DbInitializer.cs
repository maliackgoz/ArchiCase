using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Infrastructure.Utilities;

namespace SubscriptionApp.Infrastructure.Persistence;

/// <summary>
/// Seeds a demo dataset designed to exercise every visible state of the app:
///   - Admin account (no Customer link)
///   - Customer "Ahmet" — fully caught up; auto-pay everywhere; clean dashboard, no reminders.
///   - Customer "Fatma" — manual payer; one overdue + one due-soon reminder, one failed-then-retried history.
///   - Customer "Mehmet" — mixed: one sub due today (manual), one auto-pay, one Passive with a failed payment.
/// All payment days fall inside the provider's [billing, billing+7] window.
/// </summary>
public static class DbInitializer
{
    private const string DefaultPassword = "Test1234!";
    private const string AdminPassword = "Admin1234!";
    private const int PaymentWindowDays = 7;

    // Use Turkey-local "today" so seed payment days line up with what the customer sees.
    private static readonly DateTime SeedToday = Utilities.BusinessClock.Now();

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var context = serviceProvider.GetRequiredService<AppDbContext>();

        if (await context.Customers.AnyAsync() || await context.Users.AnyAsync())
            return;

        // ── Customers ────────────────────────────────────────────────────────
        var ahmet = new Customer
        {
            FullName = "Ahmet Yılmaz",
            Email = "ahmet.yilmaz@example.com",
            PhoneNumber = "+905551234567",
            CreatedAt = SeedToday.AddMonths(-9)
        };
        var fatma = new Customer
        {
            FullName = "Fatma Kaya",
            Email = "fatma.kaya@example.com",
            PhoneNumber = "+905559876543",
            CreatedAt = SeedToday.AddMonths(-7)
        };
        var mehmet = new Customer
        {
            FullName = "Mehmet Demir",
            Email = "mehmet.demir@example.com",
            PhoneNumber = "+905553456789",
            CreatedAt = SeedToday.AddMonths(-5)
        };
        context.Customers.AddRange(ahmet, fatma, mehmet);
        await context.SaveChangesAsync();

        // ── Subscriptions ────────────────────────────────────────────────────
        // Helper: build a subscription scenario where the payment day is targeted relative to today.
        // We pick a billingDay such that (billing, billing+7) contains the desired payment day,
        // then clamp into [1, 21] for billing so lastPayment ∈ [8, 28].
        Subscription Make(
            int customerId, SubscriptionType type, string provider, string number,
            int desiredPaymentDay, bool isAutoPay,
            SubscriptionStatus status = SubscriptionStatus.Active,
            DateTime? createdAt = null)
        {
            // Bring desiredPaymentDay into a valid range for the billing math.
            var payDay = Math.Clamp(desiredPaymentDay, 8, 28);
            // billingDay = payDay - some offset within the 7-day window, kept in [1, 21].
            var offset = (payDay <= 14) ? Math.Min(payDay - 1, PaymentWindowDays - 1) : PaymentWindowDays;
            var billing = Math.Clamp(payDay - offset, 1, 21);
            var lastPayment = billing + PaymentWindowDays;
            var clampedPay = Math.Clamp(payDay, billing, lastPayment);

            return new Subscription
            {
                CustomerId = customerId,
                SubscriptionType = type,
                ProviderName = provider,
                SubscriptionNumber = number,
                Status = status,
                BillingDayOfMonth = billing,
                LastPaymentDayOfMonth = lastPayment,
                PaymentDayOfMonth = clampedPay,
                IsAutoPay = isAutoPay,
                CreatedAt = createdAt ?? SeedToday.AddMonths(-6)
            };
        }

        var today = SeedToday.Day;

        // Ahmet — caught up; auto-pay on across the board.
        var ahmetSubs = new[]
        {
            Make(ahmet.Id, SubscriptionType.Electricity, "BEDAŞ", "BDS-A-1001",
                desiredPaymentDay: 18, isAutoPay: true, createdAt: SeedToday.AddMonths(-9)),
            Make(ahmet.Id, SubscriptionType.Internet, "Türk Telekom", "TT-NET-101",
                desiredPaymentDay: 12, isAutoPay: true, createdAt: SeedToday.AddMonths(-9)),
            Make(ahmet.Id, SubscriptionType.Gsm, "Turkcell", "TC-555-1010",
                desiredPaymentDay: 25, isAutoPay: true, createdAt: SeedToday.AddMonths(-8)),
        };

        // Fatma — manual payer; rolling reminders.
        //   • İSKİ Water: last payment day in 2 days → "due soon"
        //   • BEDAŞ Electricity: last payment day was 3 days ago → "overdue"
        //   • İGDAŞ Natural Gas: last payment day in 14 days → no reminder yet
        var fatmaSubs = new[]
        {
            Make(fatma.Id, SubscriptionType.Water, "İSKİ", "ISKI-FK-2001",
                desiredPaymentDay: Math.Min(28, today + 2), isAutoPay: false, createdAt: SeedToday.AddMonths(-6)),
            Make(fatma.Id, SubscriptionType.Electricity, "BEDAŞ", "BDS-FK-2002",
                desiredPaymentDay: Math.Max(8, today - 3), isAutoPay: false, createdAt: SeedToday.AddMonths(-6)),
            Make(fatma.Id, SubscriptionType.NaturalGas, "İGDAŞ", "IGDAS-FK-2003",
                desiredPaymentDay: Math.Min(28, today + 14), isAutoPay: false, createdAt: SeedToday.AddMonths(-5)),
        };

        // Mehmet — mixed states.
        //   • Vodafone GSM: last payment day = today → "due today" (manual; he might forget)
        //   • Vodafone Net: payment day = today + 9 with auto-pay ON → currently unpaid but will be auto-charged
        //   • BEDAŞ Old: Passive with one historical failure
        var mehmetSubs = new[]
        {
            Make(mehmet.Id, SubscriptionType.Gsm, "Vodafone", "VD-MD-7000",
                desiredPaymentDay: Math.Max(8, today), isAutoPay: false, createdAt: SeedToday.AddMonths(-4)),
            Make(mehmet.Id, SubscriptionType.Internet, "Vodafone Net", "VDN-MD-002",
                desiredPaymentDay: Math.Min(28, today + 9), isAutoPay: true, createdAt: SeedToday.AddMonths(-4)),
            Make(mehmet.Id, SubscriptionType.Electricity, "BEDAŞ", "BDS-MD-OLD-001",
                desiredPaymentDay: 8, isAutoPay: false,
                status: SubscriptionStatus.Passive, createdAt: SeedToday.AddMonths(-5)),
        };

        context.Subscriptions.AddRange(ahmetSubs);
        context.Subscriptions.AddRange(fatmaSubs);
        context.Subscriptions.AddRange(mehmetSubs);
        await context.SaveChangesAsync();

        // ── Users (one per customer + an admin) ──────────────────────────────
        context.Users.AddRange(
            new User { Email = ahmet.Email,  PasswordHash = PasswordHasher.Hash(DefaultPassword), Role = "Customer", CustomerId = ahmet.Id,  CreatedAt = ahmet.CreatedAt },
            new User { Email = fatma.Email,  PasswordHash = PasswordHasher.Hash(DefaultPassword), Role = "Customer", CustomerId = fatma.Id,  CreatedAt = fatma.CreatedAt },
            new User { Email = mehmet.Email, PasswordHash = PasswordHasher.Hash(DefaultPassword), Role = "Customer", CustomerId = mehmet.Id, CreatedAt = mehmet.CreatedAt },
            new User { Email = "admin@bank.com", PasswordHash = PasswordHasher.Hash(AdminPassword), Role = "Admin", CustomerId = null, CreatedAt = SeedToday.AddMonths(-12) }
        );
        await context.SaveChangesAsync();

        // ── Payment history ─────────────────────────────────────────────────
        var payments = new List<Payment>();

        var monthAnchors = Enumerable.Range(1, 6)
            .Select(i => new DateTime(SeedToday.Year, SeedToday.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i))
            .Reverse()
            .ToArray();
        var currentMonthAnchor = new DateTime(SeedToday.Year, SeedToday.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Ahmet: every subscription paid every month including the current one.
        foreach (var sub in ahmetSubs)
        {
            foreach (var anchor in monthAnchors)
                payments.Add(NewSuccessfulPayment(sub, anchor));
            payments.Add(NewSuccessfulPayment(sub, currentMonthAnchor));
        }

        // Fatma:
        //   Water + Natural Gas: paid the last 3 full months, NOT current month → reminder/dashboard candidates.
        //   Electricity: paid Feb + Mar, then 1 failed attempt + successful retry in April, NOT current month.
        var fatmaWater = fatmaSubs[0];
        var fatmaElec  = fatmaSubs[1];
        var fatmaGas   = fatmaSubs[2];

        foreach (var anchor in monthAnchors.TakeLast(3))
        {
            payments.Add(NewSuccessfulPayment(fatmaWater, anchor, amount: 142.50m));
            payments.Add(NewSuccessfulPayment(fatmaGas,  anchor, amount: 268.75m));
        }
        foreach (var anchor in monthAnchors.TakeLast(3).Take(2))
            payments.Add(NewSuccessfulPayment(fatmaElec, anchor, amount: 215.00m));

        var aprAnchor = monthAnchors.Last();
        payments.Add(NewFailedPayment(fatmaElec, aprAnchor, amount: 215.00m, retryAttempt: 1));
        payments.Add(NewSuccessfulPayment(fatmaElec, aprAnchor, amount: 215.00m, retryAttempt: 2));

        // Mehmet:
        //   GSM + Internet: paid the last 4 months, nothing current.
        //   Old Electricity (Passive): one historical failure.
        var mehmetGsm = mehmetSubs[0];
        var mehmetNet = mehmetSubs[1];
        var mehmetOld = mehmetSubs[2];

        foreach (var anchor in monthAnchors.TakeLast(4))
        {
            payments.Add(NewSuccessfulPayment(mehmetGsm, anchor, amount: 189.00m));
            payments.Add(NewSuccessfulPayment(mehmetNet, anchor, amount: 199.90m));
        }
        payments.Add(NewFailedPayment(mehmetOld, monthAnchors[0], amount: 312.40m));

        context.Payments.AddRange(payments);
        await context.SaveChangesAsync();
    }

    private static Payment NewSuccessfulPayment(
        Subscription sub, DateTime monthAnchor, decimal? amount = null, int retryAttempt = 1)
    {
        var amt = amount ?? DefaultAmountFor(sub.SubscriptionType);
        var payDay = Math.Min(sub.PaymentDayOfMonth, 28);
        var paymentDate = new DateTime(monthAnchor.Year, monthAnchor.Month, payDay, 14, 0, 0, DateTimeKind.Utc)
            .AddHours(retryAttempt - 1);
        return new Payment
        {
            SubscriptionId = sub.Id,
            Amount = amt,
            Period = monthAnchor.ToString("yyyy-MM"),
            PaymentDate = paymentDate,
            Status = PaymentStatus.Successful,
            ExternalTransactionId = $"TXN-{Guid.NewGuid()}"
        };
    }

    private static Payment NewFailedPayment(
        Subscription sub, DateTime monthAnchor, decimal? amount = null, int retryAttempt = 1)
    {
        var amt = amount ?? DefaultAmountFor(sub.SubscriptionType);
        var payDay = Math.Min(sub.PaymentDayOfMonth, 28);
        var paymentDate = new DateTime(monthAnchor.Year, monthAnchor.Month, payDay, 13, 0, 0, DateTimeKind.Utc)
            .AddMinutes(15 * retryAttempt);
        return new Payment
        {
            SubscriptionId = sub.Id,
            Amount = amt,
            Period = monthAnchor.ToString("yyyy-MM"),
            PaymentDate = paymentDate,
            Status = PaymentStatus.Failed,
            ExternalTransactionId = null
        };
    }

    private static decimal DefaultAmountFor(SubscriptionType type) => type switch
    {
        SubscriptionType.Electricity => 220.00m,
        SubscriptionType.Water       => 95.50m,
        SubscriptionType.Internet    => 199.90m,
        SubscriptionType.Gsm         => 189.00m,
        SubscriptionType.NaturalGas  => 275.25m,
        _ => 150.00m
    };
}
