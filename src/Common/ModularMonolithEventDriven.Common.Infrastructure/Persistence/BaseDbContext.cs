using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Common.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditableProperties();
        return await base.SaveChangesAsync(cancellationToken);
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
