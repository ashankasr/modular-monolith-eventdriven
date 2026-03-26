using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;

public sealed class StockReservationRepository(InventoryDbContext context)
    : BaseRepository<StockReservation, Guid, InventoryDbContext>(context), IStockReservationRepository
{
    public async Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await Context.StockReservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
}
