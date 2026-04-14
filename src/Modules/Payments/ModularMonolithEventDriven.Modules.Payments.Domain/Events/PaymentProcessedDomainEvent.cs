using System.Text.Json.Serialization;
using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Payments.Domain.Events;

public sealed record PaymentProcessedDomainEvent : DomainEvent
{
    public Guid CorrelationId { get; init; }
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }

    [JsonConstructor]
    public PaymentProcessedDomainEvent(
        Guid EventId, DateTime OccurredOn, Guid CorrelationId, Guid OrderId, Guid PaymentId, decimal Amount)
        : base(EventId, OccurredOn)
    {
        this.CorrelationId = CorrelationId;
        this.OrderId = OrderId;
        this.PaymentId = PaymentId;
        this.Amount = Amount;
    }

    public PaymentProcessedDomainEvent(Guid correlationId, Guid orderId, Guid paymentId, decimal amount)
        : this(Guid.NewGuid(), DateTime.UtcNow, correlationId, orderId, paymentId, amount) { }
}
