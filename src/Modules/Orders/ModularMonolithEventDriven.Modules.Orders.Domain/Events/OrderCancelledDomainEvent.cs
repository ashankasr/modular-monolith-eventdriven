using System.Text.Json.Serialization;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Orders.Domain.Events;

public sealed record OrderCancelledDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; }

    [JsonConstructor]
    public OrderCancelledDomainEvent(
        Guid EventId, DateTime OccurredOn, Guid OrderId, string Reason)
        : base(EventId, OccurredOn)
    {
        this.OrderId = OrderId;
        this.Reason = Reason;
    }

    public OrderCancelledDomainEvent(Guid orderId, string reason)
        : this(Guid.NewGuid(), DateTime.UtcNow, orderId, reason) { }
}
