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

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(p => p.Id);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            b.Property(p => p.Price).HasPrecision(18, 2);
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<StockReservation>(b =>
        {
            b.ToTable("StockReservations");
            b.HasKey(r => r.Id);
            b.Property(r => r.Status).HasConversion<string>();
            b.OwnsMany(r => r.Items, ib =>
            {
                ib.ToTable("StockReservationItems");
                ib.WithOwner().HasForeignKey("ReservationId");
                ib.Property(i => i.ProductId);
                ib.Property(i => i.Quantity);
            });
        });
    }
}
