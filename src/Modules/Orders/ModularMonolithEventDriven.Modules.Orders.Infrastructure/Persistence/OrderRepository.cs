using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.Domain;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;

public sealed class OrderRepository(OrdersDbContext context)
    : BaseRepository<Order, Guid, OrdersDbContext>(context), IOrderRepository
{
    public override async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Context.Orders
            .Include(o => o.Items)
            .ToListAsync(cancellationToken);
}
