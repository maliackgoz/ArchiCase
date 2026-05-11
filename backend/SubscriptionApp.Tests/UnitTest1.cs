using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.ExternalServices;
using SubscriptionApp.Infrastructure.ExternalServices.Models;
using SubscriptionApp.Infrastructure.Persistence;
using SubscriptionApp.Infrastructure.Services;

namespace SubscriptionApp.Tests;

// ── Test doubles ─────────────────────────────────────────────────────────────

/// <summary>Always succeeds with a deterministic transaction ID.</summary>
file class SuccessGatewayStub : IPaymentGatewayClient
{
    public Task<PaymentGatewayResponse> ProcessPaymentAsync(int subscriptionId, decimal amount)
        => Task.FromResult(new PaymentGatewayResponse { Success = true, TransactionId = "TXN-TEST" });
}

/// <summary>No-op — prevents real HTTP calls during tests.</summary>
file class NoopNotificationStub : INotificationClient
{
    public Task SendAsync(string channel, string recipient, string message)
        => Task.CompletedTask;
}

// ── Shared helpers ────────────────────────────────────────────────────────────

file static class TestHelpers
{
    /// <summary>Fresh InMemory context with a unique DB name — guarantees test isolation.
    /// TransactionIgnoredWarning is suppressed: InMemory transactions are no-ops, which is
    /// intentional in tests — we're testing business logic, not transaction rollback.</summary>
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    public static Customer SeedCustomer(AppDbContext db)
    {
        var customer = new Customer
        {
            FullName = "Test User",
            Email = "test@example.com",
            PhoneNumber = "+905551234567",
            CreatedAt = DateTime.UtcNow
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        return customer;
    }

    public static Subscription SeedSubscription(AppDbContext db, int customerId,
        SubscriptionStatus status = SubscriptionStatus.Active)
    {
        var sub = new Subscription
        {
            CustomerId = customerId,
            SubscriptionType = SubscriptionType.Internet,
            ProviderName = "TestProvider",
            SubscriptionNumber = Guid.NewGuid().ToString(),
            Status = status,
            BillingDayOfMonth = 10,
            CreatedAt = DateTime.UtcNow
        };
        db.Subscriptions.Add(sub);
        db.SaveChanges();
        return sub;
    }

    public static PaymentService CreatePaymentService(AppDbContext db)
        => new(db, new SuccessGatewayStub(), new NoopNotificationStub());
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class PaymentServiceTests
{
    [Fact]
    public async Task PaymentService_RejectsDuplicateSuccessfulPaymentForSamePeriod()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryContext();
        var customer = TestHelpers.SeedCustomer(db);
        var sub = TestHelpers.SeedSubscription(db, customer.Id);

        // Seed an existing Successful payment for 2026-05
        db.Payments.Add(new Payment
        {
            SubscriptionId = sub.Id,
            Amount = 100m,
            Period = "2026-05",
            Status = PaymentStatus.Successful,
            PaymentDate = DateTime.UtcNow
        });
        db.SaveChanges();

        var service = TestHelpers.CreatePaymentService(db);

        // Act — second payment for the same subscription + period
        var act = () => service.CreateAsync(sub.Id, 100m, "2026-05");

        // Assert
        await act.Should().ThrowAsync<DuplicatePaymentException>()
            .WithMessage("*2026-05*");
    }

    [Fact]
    public async Task PaymentService_RejectsPaymentOnPassiveSubscription()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryContext();
        var customer = TestHelpers.SeedCustomer(db);
        var sub = TestHelpers.SeedSubscription(db, customer.Id, SubscriptionStatus.Passive);

        var service = TestHelpers.CreatePaymentService(db);

        // Act
        var act = () => service.CreateAsync(sub.Id, 100m, "2026-05");

        // Assert
        await act.Should().ThrowAsync<InactiveSubscriptionException>()
            .WithMessage($"*{sub.Id}*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999.99)]
    public async Task PaymentService_RejectsZeroOrNegativeAmount(decimal invalidAmount)
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryContext();
        var customer = TestHelpers.SeedCustomer(db);
        var sub = TestHelpers.SeedSubscription(db, customer.Id);
        var service = TestHelpers.CreatePaymentService(db);

        // Act
        var act = () => service.CreateAsync(sub.Id, invalidAmount, "2026-05");

        // Assert — amount guard throws before any DB or gateway call
        await act.Should().ThrowAsync<DomainException>()
            .Where(ex => ex.Code == "INVALID_AMOUNT");
    }
}

public class DashboardTests
{
    [Fact]
    public async Task CustomerDashboard_CorrectlyIdentifiesUnpaidThisMonthSubscriptions()
    {
        // Arrange
        using var db = TestHelpers.CreateInMemoryContext();
        var customer = TestHelpers.SeedCustomer(db);
        var currentPeriod = DateTime.UtcNow.ToString("yyyy-MM");

        // Sub A — Active, paid this month → must NOT be in UnpaidThisMonth
        var subPaid = TestHelpers.SeedSubscription(db, customer.Id);
        db.Payments.Add(new Payment
        {
            SubscriptionId = subPaid.Id,
            Amount = 150m,
            Period = currentPeriod,
            Status = PaymentStatus.Successful,
            PaymentDate = DateTime.UtcNow
        });

        // Sub B — Active, paid last month only → must be in UnpaidThisMonth
        var subOldPayment = TestHelpers.SeedSubscription(db, customer.Id);
        db.Payments.Add(new Payment
        {
            SubscriptionId = subOldPayment.Id,
            Amount = 80m,
            Period = "2026-04",
            Status = PaymentStatus.Successful,
            PaymentDate = DateTime.UtcNow.AddMonths(-1)
        });

        // Sub C — Active, never paid → must be in UnpaidThisMonth
        var subNeverPaid = TestHelpers.SeedSubscription(db, customer.Id);

        db.SaveChanges();

        var service = new CustomerService(db);

        // Act
        var dashboard = await service.GetDashboardAsync(customer.Id);

        // Assert
        dashboard.ActiveSubscriptionCount.Should().Be(3);
        dashboard.UnpaidThisMonth.Should().HaveCount(2);
        dashboard.UnpaidThisMonth.Select(s => s.Id).Should()
            .Contain(subOldPayment.Id).And.Contain(subNeverPaid.Id);
        dashboard.UnpaidThisMonth.Select(s => s.Id).Should()
            .NotContain(subPaid.Id);
        dashboard.TotalPaidThisYear.Should().Be(230m); // 150 + 80
        dashboard.RecentPayments.Should().HaveCount(2);
    }
}
