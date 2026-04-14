using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Orders.Domain.Events;

public sealed record OrderCancelledDomainEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid OrderId,
    string Reason) : DomainEvent(EventId, OccurredOn)
{
    public OrderCancelledDomainEvent(Guid orderId, string reason)
        : this(Guid.NewGuid(), DateTime.UtcNow, orderId, reason) { }
}
