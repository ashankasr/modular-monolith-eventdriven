using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Notifications.Domain;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Consumers;

// CHOREOGRAPHY: Reacts autonomously to OrderCancelledEvent — sends cancellation confirmation
public sealed class OrderCancelledNotificationConsumer(
    NotificationsDbContext dbContext,
    ILogger<OrderCancelledNotificationConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Notifications received OrderCancelledEvent for Order {OrderId}", msg.OrderId);

        var log = new NotificationLog(
            Guid.NewGuid(),
            msg.OrderId,
            "customer@example.com",
            "Cancelled",
            $"[CHOREOGRAPHY] Your order {msg.OrderId} has been cancelled. Reason: {msg.Reason}");

        dbContext.NotificationLogs.Add(log);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] ✓ Cancellation notification sent for Order {OrderId}", msg.OrderId);
    }
}
