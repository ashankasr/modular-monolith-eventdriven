using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Notifications.Domain;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : BaseDbContext(options)
{
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<NotificationLog>(b =>
        {
            b.ToTable("NotificationLogs");
            b.HasKey(n => n.Id);
            b.Property(n => n.RecipientEmail).IsRequired().HasMaxLength(200);
            b.Property(n => n.Status).IsRequired().HasMaxLength(50);
            b.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        });
    }
}
