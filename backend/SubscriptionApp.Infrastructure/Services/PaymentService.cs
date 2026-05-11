using Microsoft.EntityFrameworkCore;
using SubscriptionApp.Domain.Entities;
using SubscriptionApp.Domain.Enums;
using SubscriptionApp.Domain.Exceptions;
using SubscriptionApp.Infrastructure.ExternalServices;
using SubscriptionApp.Infrastructure.Persistence;

namespace SubscriptionApp.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _db;
    private readonly IPaymentGatewayClient _gatewayClient;
    private readonly INotificationClient _notificationClient;

    public PaymentService(
        AppDbContext db,
        IPaymentGatewayClient gatewayClient,
        INotificationClient notificationClient)
    {
        _db = db;
        _gatewayClient = gatewayClient;
        _notificationClient = notificationClient;
    }

    public async Task<List<Payment>> GetAllAsync(int? subscriptionId)
    {
        var query = _db.Payments.AsQueryable();

        if (subscriptionId.HasValue)
            query = query.Where(p => p.SubscriptionId == subscriptionId.Value);

        return await query.OrderByDescending(p => p.PaymentDate).ToListAsync();
    }

    public async Task<Payment> GetByIdAsync(int id)
    {
        var payment = await _db.Payments.FindAsync(id);

        if (payment is null)
            throw new NotFoundException(nameof(Payment), id);

        return payment;
    }

    public async Task<Payment> CreateAsync(int subscriptionId, decimal amount, string period)
    {
        // A transaction ensures the payment record write is atomic with the pre-checks.
        // This protects against partial state if the process crashes between the gateway
        // call and the DB write, and keeps a clean audit trail for every attempt.
        await using var transaction = await _db.Database.BeginTransactionAsync();
        bool committed = false;

        try
        {
            // Step 1: Load subscription with customer (customer needed for notification recipient).
            var subscription = await _db.Subscriptions
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);

            if (subscription is null)
                throw new NotFoundException(nameof(Subscription), subscriptionId);

            // Step 2: Passive subscriptions cannot receive payments.
            if (subscription.Status == SubscriptionStatus.Passive)
                throw new InactiveSubscriptionException(subscriptionId);

            // Step 3 (App-level pre-check): reject duplicate before hitting the gateway.
            // The DB filtered unique index on (SubscriptionId, Period) WHERE Status=0 is the
            // ultimate safety net for concurrent requests — this pre-check just gives a
            // friendly error message instead of a raw DB constraint violation.
            var alreadyPaid = await _db.Payments.AnyAsync(p =>
                p.SubscriptionId == subscriptionId &&
                p.Period == period &&
                p.Status == PaymentStatus.Successful);

            if (alreadyPaid)
                throw new DuplicatePaymentException(subscriptionId, period);

            // Step 4: Call payment gateway.
            var gatewayResponse = await _gatewayClient.ProcessPaymentAsync(subscriptionId, amount);

            // Step 5: Record the payment attempt regardless of outcome (audit trail).
            var payment = new Payment
            {
                SubscriptionId = subscriptionId,
                // decimal — never double/float. Stored as decimal(18,2) in the DB.
                Amount = amount,
                Period = period,
                // DateTime.UtcNow — never DateTime.Now. All timestamps are UTC.
                PaymentDate = DateTime.UtcNow,
                Status = gatewayResponse.Success ? PaymentStatus.Successful : PaymentStatus.Failed,
                ExternalTransactionId = gatewayResponse.Success ? gatewayResponse.TransactionId : null
            };

            _db.Payments.Add(payment);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            committed = true;

            if (gatewayResponse.Success)
            {
                // Fire-and-forget: notification is non-critical. A failure must NOT roll back
                // the already-committed payment record or block the HTTP response.
                var phone = subscription.Customer.PhoneNumber;
                var msg = $"Payment of {amount:F2} TRY accepted for period {period}. Ref: {payment.ExternalTransactionId}";
                _ = Task.Run(async () =>
                {
                    try { await _notificationClient.SendAsync("SMS", phone, msg); }
                    catch { /* swallow — notification failure is non-critical */ }
                });

                return payment;
            }

            // Gateway declined: the Failed record is committed; now surface the error to the caller.
            throw new ExternalServiceException(gatewayResponse.ErrorCode ?? "UNKNOWN_ERROR");
        }
        catch
        {
            // Roll back only if we haven't committed yet (domain rule violations in steps 1–3,
            // unexpected exceptions during the gateway call or SaveChanges).
            // After CommitAsync the transaction is closed; rolling back would throw.
            if (!committed)
                await transaction.RollbackAsync();

            throw;
        }
    }
}
