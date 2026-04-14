using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.Domain;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Consumers;

// ORCHESTRATION: Responds to ReserveStockCommand from the Saga
public sealed class ReserveStockCommandConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<ReserveStockCommandConsumer> logger) : IConsumer<ReserveStockCommand>
{
    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var msg = context.Message;
        logger.LogInformation("[ORCHESTRATION] Inventory received ReserveStockCommand for Order {OrderId}", msg.OrderId);

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
                logger.LogWarning("[ORCHESTRATION] Stock reservation FAILED for Order {OrderId}. {Reason}", msg.OrderId, reason);
                await context.Publish(new StockReservationFailedEvent(msg.CorrelationId, msg.OrderId, reason, DateTime.UtcNow));
                return;
            }
        }

        foreach (var item in msg.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);
            product.ReserveStock(item.Quantity);
        }

        var reservationId = Guid.NewGuid();
        var reservationItems = msg.Items.Select(i => new ReservationItem(i.ProductId, i.Quantity)).ToList();
        var reservationResult = StockReservation.Create(reservationId, msg.OrderId, msg.CorrelationId, reservationItems);
        if (reservationResult.IsFailure)
        {
            logger.LogError("[ORCHESTRATION] Failed to create reservation for Order {OrderId}. {Reason}", msg.OrderId, reservationResult.Error.Description);
            await context.Publish(new StockReservationFailedEvent(msg.CorrelationId, msg.OrderId, reservationResult.Error.Description, DateTime.UtcNow));
            return;
        }

        reservationRepository.Add(reservationResult.Value);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        // StockReservedEvent is published by StockReservedDomainEventHandler via the outbox

        logger.LogInformation("[ORCHESTRATION] Stock reserved for Order {OrderId}. ReservationId: {ReservationId}", msg.OrderId, reservationId);
    }
}
