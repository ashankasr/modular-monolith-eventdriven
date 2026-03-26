using MassTransit;
using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Domain;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : BaseDbContext(options), IOrdersUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerId).IsRequired().HasMaxLength(100);
            b.Property(o => o.CustomerEmail).IsRequired().HasMaxLength(200);
            b.Property(o => o.Status).HasConversion<string>().IsRequired();
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.Property(o => o.FailureReason).HasMaxLength(500);
            b.HasMany(o => o.Items)
             .WithOne()
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(b =>
        {
            b.ToTable("OrderItems");
            b.HasKey(i => i.Id);
            b.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
            b.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });

        // Saga state machine persistence
        modelBuilder.Entity<OrderSagaState>(b =>
        {
            b.ToTable("OrderSagaState");
            b.HasKey(s => s.CorrelationId);
            b.Property(s => s.CurrentState).IsRequired().HasMaxLength(64);
            b.Property(s => s.CustomerId).IsRequired().HasMaxLength(100);
            b.Property(s => s.CustomerEmail).IsRequired().HasMaxLength(200);
            b.Property(s => s.TotalAmount).HasPrecision(18, 2);
            b.Property(s => s.FailureReason).HasMaxLength(500);
        });
    }
}
