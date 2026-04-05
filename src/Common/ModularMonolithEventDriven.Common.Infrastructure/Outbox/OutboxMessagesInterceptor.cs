using Microsoft.EntityFrameworkCore.Diagnostics;
using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Infrastructure.Persistence;
using System.Text.Json;

namespace ModularMonolithEventDriven.Common.Infrastructure.Outbox;

/// <summary>
/// EF Core SaveChanges interceptor.
/// Runs inside the same DB transaction as the aggregate save:
///   1. Collects domain events from all tracked IHasDomainEvents entities
///   2. Serialises each to an OutboxMessage row (same schema as the owning DbContext)
///   3. Clears the in-memory domain events list from the entity
/// </summary>
public sealed class OutboxMessagesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is BaseDbContext context)
            ConvertDomainEventsToOutboxMessages(context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ConvertDomainEventsToOutboxMessages(BaseDbContext context)
    {
        var entitiesWithEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var outboxMessages = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Type = domainEvent.GetType().AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
            })
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);

        entitiesWithEvents.ForEach(e => e.ClearDomainEvents());
    }
}
