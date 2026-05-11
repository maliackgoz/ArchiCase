using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        // decimal(18,2): banking standard precision. Never float/double for money.
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentDate)
            .IsRequired();

        builder.Property(p => p.Period)
            .IsRequired()
            .HasMaxLength(7); // YYYY-MM = exactly 7 chars. Regex validation happens in the service layer.
        // Decision: period format (YYYY-MM) is validated in PaymentService, not via a DB CHECK constraint.
        // SQL Server supports LIKE-based CHECK constraints but cannot fully enforce the month range (01-12).
        // Service-layer validation is simpler and sufficient given the layered-defense approach.

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.ExternalTransactionId)
            .HasMaxLength(100); // nullable — null means the payment attempt never reached the gateway

        // Layered defense for duplicate payment rule (SPEC.md rule #1):
        // - PaymentService pre-checks for an existing Successful payment (gives a nice error message).
        // - This filtered unique index is the ultimate safety net against concurrent inserts.
        // The filter [Status] = 0 targets only Successful payments (PaymentStatus.Successful = 0).
        // Failed retries for the same period are allowed and useful for audit purposes.
        builder.HasIndex(p => new { p.SubscriptionId, p.Period })
            .IsUnique()
            .HasFilter("[Status] = 0");

        // Cascade delete: removing a Subscription removes all its Payment records.
        builder.HasOne(p => p.Subscription)
            .WithMany(s => s.Payments)
            .HasForeignKey(p => p.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
