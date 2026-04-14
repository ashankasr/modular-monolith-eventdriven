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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
