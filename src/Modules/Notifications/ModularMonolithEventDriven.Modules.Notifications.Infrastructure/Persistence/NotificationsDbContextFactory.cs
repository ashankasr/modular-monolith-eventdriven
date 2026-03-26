using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContextFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NotificationsDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Integrated Security=false;User ID=sa;Password=Donkey@1;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "notifications"));

        return new NotificationsDbContext(optionsBuilder.Options);
    }
}
