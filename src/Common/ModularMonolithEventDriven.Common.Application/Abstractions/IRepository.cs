using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Common.Application.Abstractions;

public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
