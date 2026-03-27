using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.Domain;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Consumers;

// ORCHESTRATION: Handles ReleaseStockCommand (compensating transaction)
public sealed class ReleaseStockCommandConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<ReleaseStockCommandConsumer> logger) : IConsumer<ReleaseStockCommand>
{
    public async Task Consume(ConsumeContext<ReleaseStockCommand> context)
    {
        var msg = context.Message;
        logger.LogInformation("[ORCHESTRATION] Releasing stock for Order {OrderId}, ReservationId: {ReservationId}", msg.OrderId, msg.ReservationId);

        var reservation = await reservationRepository.GetByIdAsync(msg.ReservationId, context.CancellationToken);
        if (reservation is null) return;

        var productIds = reservation.Items.Select(i => i.ProductId).ToList();
        var products = await productRepository.GetByIdsAsync(productIds, context.CancellationToken);

        foreach (var item in reservation.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            product?.ReleaseStock(item.Quantity);
        }

        reservation.Release();
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[ORCHESTRATION] Stock released for Order {OrderId}", msg.OrderId);
        await context.Publish(new StockReleasedEvent(msg.CorrelationId, msg.OrderId, msg.ReservationId, DateTime.UtcNow));
    }
}
