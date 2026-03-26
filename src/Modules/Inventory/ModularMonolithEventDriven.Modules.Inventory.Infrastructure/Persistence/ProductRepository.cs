using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;

public sealed class ProductRepository(InventoryDbContext context)
    : BaseRepository<Product, Guid, InventoryDbContext>(context), IProductRepository
{
    public async Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default) =>
        await Context.Products.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);

    public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Context.Products.ToListAsync(cancellationToken);
}
