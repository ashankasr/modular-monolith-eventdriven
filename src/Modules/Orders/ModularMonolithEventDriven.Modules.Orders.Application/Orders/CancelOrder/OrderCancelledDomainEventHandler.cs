using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain.Events;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.CancelOrder;

public sealed class OrderCancelledDomainEventHandler(IBus bus)
    : IDomainEventHandler<OrderCancelledDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<OrderCancelledDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        await bus.Publish(
            new OrderCancelledEvent(e.OrderId, e.Reason, e.OccurredOn),
            cancellationToken);
    }
}
