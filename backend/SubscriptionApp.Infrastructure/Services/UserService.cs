using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.ExternalServices;
using SubscriptionApp.Infrastructure.Persistence;
using SubscriptionApp.Infrastructure.Utilities;

namespace SubscriptionApp.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly INotificationClient _notificationClient;
    private readonly IDebtInquiryClient _debtInquiryClient;
    private readonly IPaymentService _paymentService;

    public UserService(
        AppDbContext db,
        INotificationClient notificationClient,
        IDebtInquiryClient debtInquiryClient,
        IPaymentService paymentService)
    {
        _db = db;
        _notificationClient = notificationClient;
        _debtInquiryClient = debtInquiryClient;
        _paymentService = paymentService;
    }

    public async Task<List<ReminderItem>> GetRemindersAsync(int customerId)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), customerId);

        var today = BusinessClock.Today();
        var currentPeriod = BusinessClock.CurrentPeriod();

        var subscriptions = await _db.Subscriptions
            .Where(s => s.CustomerId == customerId && s.Status == SubscriptionStatus.Active)
            .ToListAsync();

        var reminders = new List<ReminderItem>();

        foreach (var sub in subscriptions)
        {
            // Auto-pay subscriptions are handled by the system — the customer doesn't need a nudge.
            if (sub.IsAutoPay) continue;

            // Reminders are driven by the provider's last payment day — the real deadline.
            var daysUntilDue = sub.LastPaymentDayOfMonth - today.Day;
            if (daysUntilDue > 5) continue;

            var alreadyPaid = await _db.Payments.AnyAsync(p =>
                p.SubscriptionId == sub.Id &&
                p.Period == currentPeriod &&
                p.Status == PaymentStatus.Successful);

            if (alreadyPaid) continue;

            reminders.Add(new ReminderItem
            {
                SubscriptionId = sub.Id,
                ProviderName = sub.ProviderName,
                SubscriptionType = sub.SubscriptionType,
                BillingDayOfMonth = sub.BillingDayOfMonth,
                LastPaymentDayOfMonth = sub.LastPaymentDayOfMonth,
                PaymentDayOfMonth = sub.PaymentDayOfMonth,
                IsAutoPay = sub.IsAutoPay,
                DaysUntilDue = daysUntilDue,
                Period = currentPeriod,
                IsOverdue = daysUntilDue < 0
            });

            // Fire-and-forget reminders on both channels (SMS + Email).
            var phone = customer.PhoneNumber;
            var email = customer.Email;
            var customerName = customer.FullName;
            var provider = sub.ProviderName;
            var daysLabel = daysUntilDue < 0
                ? $"{Math.Abs(daysUntilDue)} days overdue"
                : daysUntilDue == 0 ? "due today" : $"due in {daysUntilDue} days";

            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationClient.SendAsync(
                        "SMS",
                        phone,
                        $"Reminder: Your {provider} bill for {currentPeriod} is {daysLabel}. Please pay to avoid interruption.");
                }
                catch { }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationClient.SendAsync(
                        "EMAIL",
                        email,
                        $"Hi {customerName},\n\n" +
                        $"Your {provider} subscription bill for period {currentPeriod} is {daysLabel} " +
                        $"(last payment day: {sub.LastPaymentDayOfMonth}).\n\n" +
                        "Please log in to your portal to make a payment, or enable auto-pay so we can handle it for you.\n\n" +
                        "— SubscriptionApp");
                }
                catch { }
            });
        }

        return reminders;
    }

    /// <summary>
    /// Attempts auto-payment for every Active+IsAutoPay subscription belonging to the customer
    /// where today's date has reached the customer's chosen payment day and the current period is unpaid.
    /// </summary>
    public async Task<AutoPayResult> ProcessAutoPayAsync(int customerId)
    {
        _ = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), customerId);

        var today = BusinessClock.Today();
        var currentPeriod = BusinessClock.CurrentPeriod();

        var candidates = await _db.Subscriptions
            .Where(s => s.CustomerId == customerId &&
                        s.Status == SubscriptionStatus.Active &&
                        s.IsAutoPay)
            .ToListAsync();

        var result = new AutoPayResult();

        foreach (var sub in candidates)
        {
            result.Processed++;

            // Only fire once the customer's chosen payment day has arrived.
            if (today.Day < sub.PaymentDayOfMonth)
            {
                result.Skipped++;
                continue;
            }

            var alreadyPaid = await _db.Payments.AnyAsync(p =>
                p.SubscriptionId == sub.Id &&
                p.Period == currentPeriod &&
                p.Status == PaymentStatus.Successful);

            if (alreadyPaid)
            {
                result.Skipped++;
                continue;
            }

            // Pull the live debt amount from the provider, then attempt the payment via the gateway.
            try
            {
                var debt = await _debtInquiryClient.GetDebtAsync(sub.Id);
                await _paymentService.CreateAsync(sub.Id, debt.Amount, currentPeriod);
                result.Succeeded++;
            }
            catch
            {
                result.Failed++;
            }
        }

        return result;
    }
}
