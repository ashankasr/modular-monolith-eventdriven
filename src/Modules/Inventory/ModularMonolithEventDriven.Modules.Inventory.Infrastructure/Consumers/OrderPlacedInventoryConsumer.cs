using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.Domain;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Consumers;

// CHOREOGRAPHY: Responds to OrderPlacedEvent
public sealed class OrderPlacedInventoryConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<OrderPlacedInventoryConsumer> logger) : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Inventory received OrderPlacedEvent for Order {OrderId}", msg.OrderId);

        var productIds = msg.Items.Select(i => i.ProductId).ToList();
        var products = await productRepository.GetByIdsAsync(productIds, context.CancellationToken);

        foreach (var item in msg.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null || !product.HasSufficientStock(item.Quantity))
            {
                var reason = product is null
                    ? $"Product {item.ProductId} not found"
                    : $"Insufficient stock for {product.Name}";
                logger.LogWarning("[CHOREOGRAPHY] Stock reservation FAILED for Order {OrderId}. {Reason}", msg.OrderId, reason);
                await context.Publish(new StockReservationFailedEvent(msg.OrderId, msg.OrderId, reason, DateTime.UtcNow));
                return;
            }
        }

        var reservationId = Guid.NewGuid();
        var reservationItems = msg.Items.Select(i => new ReservationItem(i.ProductId, i.Quantity)).ToList();
        var reservation = new StockReservation(reservationId, msg.OrderId, reservationItems);

        foreach (var item in msg.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.ReserveStock(item.Quantity);
        }

        reservationRepository.Add(reservation);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] Stock reserved for Order {OrderId}. ReservationId: {ReservationId}", msg.OrderId, reservationId);
        await context.Publish(new StockReservedEvent(msg.OrderId, msg.OrderId, reservationId, DateTime.UtcNow));
    }
}
