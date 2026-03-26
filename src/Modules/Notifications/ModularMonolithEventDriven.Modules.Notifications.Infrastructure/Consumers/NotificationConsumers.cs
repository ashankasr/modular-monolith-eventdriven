using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Notifications.Domain;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Consumers;

// CHOREOGRAPHY: Responds to PaymentProcessedEvent
public sealed class PaymentProcessedNotificationConsumer(
    NotificationsDbContext dbContext,
    ILogger<PaymentProcessedNotificationConsumer> logger) : IConsumer<PaymentProcessedEvent>
{
    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Notifications received PaymentProcessedEvent for Order {OrderId}. Sending confirmation.", msg.OrderId);

        var log = new NotificationLog(
            Guid.NewGuid(),
            msg.OrderId,
            "customer@example.com",
            "Completed",
            $"[CHOREOGRAPHY] Your order {msg.OrderId} has been confirmed! Payment ID: {msg.PaymentId}");

        dbContext.NotificationLogs.Add(log);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // In real life: send email/SMS here
        logger.LogInformation("[CHOREOGRAPHY] ✓ Notification sent for Order {OrderId}", msg.OrderId);
    }
}

// ORCHESTRATION: Responds to SendOrderNotificationCommand from the Saga
public sealed class SendOrderNotificationConsumer(
    NotificationsDbContext dbContext,
    ILogger<SendOrderNotificationConsumer> logger) : IConsumer<SendOrderNotificationCommand>
{
    public async Task Consume(ConsumeContext<SendOrderNotificationCommand> context)
    {
        var msg = context.Message;
        logger.LogInformation("[ORCHESTRATION] Notifications received SendOrderNotificationCommand for Order {OrderId}. Status: {Status}", msg.OrderId, msg.Status);

        var log = new NotificationLog(
            Guid.NewGuid(),
            msg.OrderId,
            msg.CustomerEmail,
            msg.Status,
            $"[ORCHESTRATION] {msg.Message}");

        dbContext.NotificationLogs.Add(log);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[ORCHESTRATION] ✓ Notification sent for Order {OrderId}. Status: {Status}", msg.OrderId, msg.Status);
    }
}
