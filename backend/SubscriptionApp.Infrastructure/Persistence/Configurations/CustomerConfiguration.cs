using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        // Unique index on Email — two customers cannot share the same email address.
        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.Property(c => c.PhoneNumber)
            .IsRequired()
            .HasMaxLength(13); // +90 followed by 10 digits = 13 chars

        // Store all timestamps as UTC; conversion to local time happens at the presentation layer.
        builder.Property(c => c.CreatedAt)
            .IsRequired();
    }
}
