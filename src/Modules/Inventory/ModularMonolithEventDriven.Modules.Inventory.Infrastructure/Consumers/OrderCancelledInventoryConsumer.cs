using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.Domain;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Consumers;

// CHOREOGRAPHY: Reacts autonomously to OrderCancelledEvent — releases reserved stock
public sealed class OrderCancelledInventoryConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<OrderCancelledInventoryConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Inventory received OrderCancelledEvent for Order {OrderId}", msg.OrderId);

        var reservation = await reservationRepository.GetByOrderIdAsync(msg.OrderId, context.CancellationToken);
        if (reservation is null)
        {
            logger.LogInformation("[CHOREOGRAPHY] No reservation found for Order {OrderId} — nothing to release", msg.OrderId);
            return;
        }

        var productIds = reservation.Items.Select(i => i.ProductId).ToList();
        var products = await productRepository.GetByIdsAsync(productIds, context.CancellationToken);

        foreach (var item in reservation.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            product?.ReleaseStock(item.Quantity);
        }

        reservation.Release();
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] Stock released for cancelled Order {OrderId}", msg.OrderId);
    }
}
