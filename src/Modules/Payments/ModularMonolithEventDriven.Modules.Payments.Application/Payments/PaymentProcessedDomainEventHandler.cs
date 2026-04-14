using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Payments.Domain.Events;
using ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Payments.Application.Payments;

public sealed class PaymentProcessedDomainEventHandler(IBus bus)
    : IDomainEventHandler<PaymentProcessedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<PaymentProcessedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        await bus.Publish(
            new PaymentProcessedEvent(e.CorrelationId, e.OrderId, e.PaymentId, e.Amount, e.OccurredOn),
            cancellationToken);
    }
}
