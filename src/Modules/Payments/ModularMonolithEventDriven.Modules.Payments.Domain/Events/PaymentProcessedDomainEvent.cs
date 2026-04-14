using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Payments.Domain.Events;

public sealed record PaymentProcessedDomainEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid CorrelationId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount) : DomainEvent(EventId, OccurredOn)
{
    public PaymentProcessedDomainEvent(Guid correlationId, Guid orderId, Guid paymentId, decimal amount)
        : this(Guid.NewGuid(), DateTime.UtcNow, correlationId, orderId, paymentId, amount) { }
}
