using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using System.Text.Json;

namespace ModularMonolithEventDriven.Common.Infrastructure.Outbox;

/// <summary>
/// Generic outbox processor — one instance per module DbContext.
/// Reads unprocessed OutboxMessage rows, deserialises to IDomainEvent,
/// wraps in DomainEventNotification&lt;T&gt; and dispatches via MediatR.
/// </summary>
public sealed class OutboxMessageProcessor<TDbContext>(
    TDbContext dbContext,
    IPublisher publisher,
    ILogger<OutboxMessageProcessor<TDbContext>> logger) : IOutboxMessageProcessor
    where TDbContext : BaseDbContext
{
    private const int BatchSize = 20;

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var eventType = System.Type.GetType(message.Type);
                if (eventType is null)
                {
                    message.Error = $"Type '{message.Type}' could not be resolved.";
                    logger.LogWarning("Outbox: type not found — {Type}", message.Type);
                    continue;
                }

                var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType);
                if (domainEvent is null)
                {
                    message.Error = "Deserialization returned null.";
                    continue;
                }

                // Wrap in DomainEventNotification<T> via reflection (keeps this generic processor
                // decoupled from concrete event types).
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

                await publisher.Publish(notification, cancellationToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox: failed to process message {Id}", message.Id);
                message.Error = ex.ToString();
            }
        }

        // Persist ProcessedOnUtc / Error updates. The interceptor runs again here but finds
        // no domain events on OutboxMessage (it doesn't implement IHasDomainEvents), so nothing
        // new is written — no infinite loop.
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
