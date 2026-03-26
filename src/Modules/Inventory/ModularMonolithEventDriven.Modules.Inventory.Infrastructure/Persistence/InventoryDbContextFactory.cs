using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Integrated Security=false;User ID=sa;Password=Donkey@1;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb",
            b => b.MigrationsHistoryTable("__EFMigrationsHistory", "inventory"));

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
