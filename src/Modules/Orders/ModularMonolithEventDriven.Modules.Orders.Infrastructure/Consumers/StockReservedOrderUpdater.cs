using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Consumers;

public sealed class StockReservedOrderUpdater(
    IOrderRepository orderRepository,
    OrdersDbContext dbContext,
    ILogger<StockReservedOrderUpdater> logger) : IConsumer<StockReservedEvent>
{
    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var order = await orderRepository.GetByIdAsync(context.Message.OrderId, context.CancellationToken);
        if (order is null) return;

        order.MarkAsStockReserved();
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("[CHOREOGRAPHY] Order {OrderId} → StockReserved", context.Message.OrderId);
    }
}
