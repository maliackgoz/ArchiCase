using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriptionApp.Domain.Entities;

namespace SubscriptionApp.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Channel).IsRequired().HasMaxLength(20);
        builder.Property(n => n.Recipient).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Message).IsRequired();
        builder.Property(n => n.SentAt).IsRequired();
        builder.HasIndex(n => n.SentAt);
    }
}
