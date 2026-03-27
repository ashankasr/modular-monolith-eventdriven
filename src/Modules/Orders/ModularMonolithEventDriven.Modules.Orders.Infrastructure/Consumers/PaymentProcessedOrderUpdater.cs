using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Consumers;

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
