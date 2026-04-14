using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Payments.Domain;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;

public sealed class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : BaseDbContext(options), IPaymentsUnitOfWork
{
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentsDbContext).Assembly);
    }
}
