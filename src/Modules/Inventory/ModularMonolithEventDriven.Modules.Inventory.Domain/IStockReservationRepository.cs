using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain;

public interface IStockReservationRepository : IRepository<StockReservation, Guid>
{
    Task<StockReservation?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
