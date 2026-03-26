using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Payments.Domain;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Consumers;

// CHOREOGRAPHY: Responds to StockReservedEvent
public sealed class StockReservedPaymentConsumer(
    IPaymentRepository paymentRepository,
    PaymentsDbContext dbContext,
    ILogger<StockReservedPaymentConsumer> logger) : IConsumer<StockReservedEvent>
{
    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Payments received StockReservedEvent for Order {OrderId}", msg.OrderId);

        // Simulate payment processing (in real life: charge credit card, etc.)
        // For demo: always succeed in choreography path
        var paymentId = Guid.NewGuid();
        var payment = Payment.Create(paymentId, msg.OrderId, "demo-customer", 0);
        paymentRepository.Add(payment);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] Payment processed for Order {OrderId}. PaymentId: {PaymentId}", msg.OrderId, paymentId);
        await context.Publish(new PaymentProcessedEvent(msg.CorrelationId, msg.OrderId, paymentId, 0, DateTime.UtcNow));
    }
}

// ORCHESTRATION: Responds to ProcessPaymentCommand from the Saga
public sealed class ProcessPaymentCommandConsumer(
    IPaymentRepository paymentRepository,
    PaymentsDbContext dbContext,
    ILogger<ProcessPaymentCommandConsumer> logger) : IConsumer<ProcessPaymentCommand>
{
    public async Task Consume(ConsumeContext<ProcessPaymentCommand> context)
    {
        var msg = context.Message;
        logger.LogInformation("[ORCHESTRATION] Payments received ProcessPaymentCommand for Order {OrderId}", msg.OrderId);

        // Demo: check if we should simulate a failure
        // In a real app, this would call a payment gateway
        // We use a simple heuristic: if Amount ends with .99, simulate failure
        if (msg.Amount % 1 == 0.99m)
        {
            logger.LogWarning("[ORCHESTRATION] Payment FAILED for Order {OrderId} (simulated)", msg.OrderId);
            await context.Publish(new PaymentFailedEvent(msg.CorrelationId, msg.OrderId, "Payment declined (simulated)", DateTime.UtcNow));
            return;
        }

        var paymentId = Guid.NewGuid();
        var payment = Payment.Create(paymentId, msg.OrderId, msg.CustomerId, msg.Amount);
        paymentRepository.Add(payment);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[ORCHESTRATION] Payment processed for Order {OrderId}. PaymentId: {PaymentId}", msg.OrderId, paymentId);
        await context.Publish(new PaymentProcessedEvent(msg.CorrelationId, msg.OrderId, paymentId, msg.Amount, DateTime.UtcNow));
    }
}
