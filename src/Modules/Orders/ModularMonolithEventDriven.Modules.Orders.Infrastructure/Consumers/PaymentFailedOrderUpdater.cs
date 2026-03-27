using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Consumers;

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
