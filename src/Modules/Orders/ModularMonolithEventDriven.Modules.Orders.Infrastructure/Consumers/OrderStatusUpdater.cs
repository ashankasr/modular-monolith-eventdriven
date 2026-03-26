using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

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

public sealed class PaymentProcessedOrderUpdater(
    IOrderRepository orderRepository,
    OrdersDbContext dbContext,
    ILogger<PaymentProcessedOrderUpdater> logger) : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var order = await orderRepository.GetByIdAsync(context.Message.OrderId, context.CancellationToken);
        if (order is null) return;

        order.MarkAsCompleted();
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogInformation("[CHOREOGRAPHY] Order {OrderId} → Completed", context.Message.OrderId);
    }
}

public sealed class PaymentFailedOrderUpdater(
    IOrderRepository orderRepository,
    OrdersDbContext dbContext,
    ILogger<PaymentFailedOrderUpdater> logger) : IConsumer<PaymentFailedEvent>
{
    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var order = await orderRepository.GetByIdAsync(context.Message.OrderId, context.CancellationToken);
        if (order is null) return;

        order.MarkAsFailed(context.Message.Reason);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        logger.LogWarning("[CHOREOGRAPHY] Order {OrderId} → Failed: {Reason}", context.Message.OrderId, context.Message.Reason);
    }
}
