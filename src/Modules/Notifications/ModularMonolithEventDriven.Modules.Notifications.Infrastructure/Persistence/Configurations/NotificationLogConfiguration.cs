using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ModularMonolithEventDriven.Modules.Notifications.Domain;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.RecipientEmail).IsRequired().HasMaxLength(200);
        builder.Property(n => n.Status).IsRequired().HasMaxLength(50);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
    }
}
