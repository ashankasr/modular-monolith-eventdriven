using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;

namespace ModularMonolithEventDriven.Common.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    // Every module DbContext inherits this DbSet. EF maps it to the schema
    // set by modelBuilder.HasDefaultSchema(...) in the derived context.
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditableProperties();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).IsRequired().HasMaxLength(500);
            b.Property(m => m.Content).IsRequired();
            b.HasIndex(m => m.ProcessedOnUtc);
        });
    }

    private void SetAuditableProperties()
    {
        var entries = ChangeTracker.Entries<AuditableEntity<Guid>>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = DateTime.UtcNow;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
