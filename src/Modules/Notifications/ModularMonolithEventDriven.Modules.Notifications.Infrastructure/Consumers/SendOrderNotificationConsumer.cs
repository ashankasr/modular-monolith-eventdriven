using MassTransit;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Modules.Notifications.Domain;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Consumers;

// ORCHESTRATION: Responds to SendOrderNotificationCommand from the Saga
public sealed class SendOrderNotificationConsumer(
    NotificationsDbContext dbContext,
    ILogger<SendOrderNotificationConsumer> logger) : IConsumer<SendOrderNotificationCommand>
{
    public async Task Consume(ConsumeContext<SendOrderNotificationCommand> context)
    {
        var msg = context.Message;
        logger.LogInformation("[ORCHESTRATION] Notifications received SendOrderNotificationCommand for Order {OrderId}. Status: {Status}", msg.OrderId, msg.Status);

        var logResult = NotificationLog.Create(
            Guid.NewGuid(),
            msg.OrderId,
            msg.CustomerEmail,
            msg.Status,
            $"[ORCHESTRATION] {msg.Message}");

        if (logResult.IsFailure)
        {
            logger.LogError("[ORCHESTRATION] Failed to create notification log for Order {OrderId}. {Reason}", msg.OrderId, logResult.Error.Description);
            return;
        }

        dbContext.NotificationLogs.Add(logResult.Value);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[ORCHESTRATION] ✓ Notification sent for Order {OrderId}. Status: {Status}", msg.OrderId, msg.Status);
    }
}
