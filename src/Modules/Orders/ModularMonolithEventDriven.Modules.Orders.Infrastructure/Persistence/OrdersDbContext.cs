using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
