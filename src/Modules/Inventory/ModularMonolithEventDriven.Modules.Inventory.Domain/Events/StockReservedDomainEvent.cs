using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain.Events;

public sealed record StockReservedDomainEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid CorrelationId,
    Guid OrderId,
    Guid ReservationId) : DomainEvent(EventId, OccurredOn)
{
    public StockReservedDomainEvent(Guid correlationId, Guid orderId, Guid reservationId)
        : this(Guid.NewGuid(), DateTime.UtcNow, correlationId, orderId, reservationId) { }
}
