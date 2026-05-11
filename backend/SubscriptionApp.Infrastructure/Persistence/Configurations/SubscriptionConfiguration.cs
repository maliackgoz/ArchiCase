using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProviderName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.SubscriptionNumber)
            .IsRequired()
            .HasMaxLength(50);

        // A subscription number must be unique within the same provider.
        builder.HasIndex(s => new { s.ProviderName, s.SubscriptionNumber })
            .IsUnique();

        builder.Property(s => s.BillingDayOfMonth)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Enum stored as int (default). Enum values are documented in SubscriptionType/SubscriptionStatus.
        builder.Property(s => s.SubscriptionType)
            .IsRequired();

        builder.Property(s => s.Status)
            .IsRequired();

        // Cascade delete: removing a Customer removes all their Subscriptions.
        // Hard delete is the chosen strategy per SPEC.md — see architecture.md for trade-off discussion.
        builder.HasOne(s => s.Customer)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
