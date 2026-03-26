using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Integrated Security=false;User ID=sa;Password=Donkey@1;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "orders"));

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
