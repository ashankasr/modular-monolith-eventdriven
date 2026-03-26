using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
}
