using System.Text.Json.Serialization;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain.Events;

public sealed record StockReleasedDomainEvent : DomainEvent
{
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public Guid ReservationId { get; init; }

    [JsonConstructor]
    public StockReleasedDomainEvent(
        Guid EventId, DateTime OccurredOn, Guid CorrelationId, Guid OrderId, Guid ReservationId)
        : base(EventId, OccurredOn)
    {
        this.CorrelationId = CorrelationId;
        this.OrderId = OrderId;
        this.ReservationId = ReservationId;
    }

    public StockReleasedDomainEvent(Guid correlationId, Guid orderId, Guid reservationId)
        : this(Guid.NewGuid(), DateTime.UtcNow, correlationId, orderId, reservationId) { }
}
