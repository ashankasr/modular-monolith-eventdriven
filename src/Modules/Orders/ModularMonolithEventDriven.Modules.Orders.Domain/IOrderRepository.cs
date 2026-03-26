using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Orders.Domain;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}
