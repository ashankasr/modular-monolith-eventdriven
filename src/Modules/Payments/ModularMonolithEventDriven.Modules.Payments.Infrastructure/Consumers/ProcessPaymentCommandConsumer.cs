using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Payments.Domain;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Consumers;

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
        var paymentResult = Payment.Create(paymentId, msg.OrderId, msg.CorrelationId, msg.CustomerId, msg.Amount);
        if (paymentResult.IsFailure)
        {
            logger.LogError("[ORCHESTRATION] Failed to create payment for Order {OrderId}. {Reason}", msg.OrderId, paymentResult.Error.Description);
            await context.Publish(new PaymentFailedEvent(msg.CorrelationId, msg.OrderId, paymentResult.Error.Description, DateTime.UtcNow));
            return;
        }

        paymentRepository.Add(paymentResult.Value);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        // PaymentProcessedEvent is published by PaymentProcessedDomainEventHandler via the outbox

        logger.LogInformation("[ORCHESTRATION] Payment processed for Order {OrderId}. PaymentId: {PaymentId}", msg.OrderId, paymentResult.Value.Id);
    }
}
