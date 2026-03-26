using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Common.Infrastructure.Persistence;

public abstract class BaseRepository<TEntity, TKey, TDbContext>(TDbContext context)
    : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
    where TDbContext : DbContext
{
    protected readonly TDbContext Context = context;
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public void Add(TEntity entity) => DbSet.Add(entity);
    public void Update(TEntity entity) => DbSet.Update(entity);
    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
