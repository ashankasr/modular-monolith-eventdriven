using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Inventory.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : BaseDbContext(options), IInventoryUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
