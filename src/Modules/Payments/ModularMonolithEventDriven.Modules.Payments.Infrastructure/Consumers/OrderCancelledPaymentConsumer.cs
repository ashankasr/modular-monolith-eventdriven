using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Payments.Domain;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Consumers;

// CHOREOGRAPHY: Reacts autonomously to OrderCancelledEvent — refunds payment if one exists
public sealed class OrderCancelledPaymentConsumer(
    IPaymentRepository paymentRepository,
    PaymentsDbContext dbContext,
    ILogger<OrderCancelledPaymentConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Payments received OrderCancelledEvent for Order {OrderId}", msg.OrderId);

        var payment = await paymentRepository.GetByOrderIdAsync(msg.OrderId, context.CancellationToken);
        if (payment is null)
        {
            logger.LogInformation("[CHOREOGRAPHY] No payment found for Order {OrderId} — nothing to refund", msg.OrderId);
            return;
        }

        payment.Refund();
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] Payment {PaymentId} refunded for cancelled Order {OrderId}", payment.Id, msg.OrderId);
    }
}
