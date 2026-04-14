using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Inventory.Domain.Events;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.StockReservations;

public sealed class StockReleasedDomainEventHandler(IBus bus)
    : IDomainEventHandler<StockReleasedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<StockReleasedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        await bus.Publish(
            new StockReleasedEvent(e.CorrelationId, e.OrderId, e.ReservationId, e.OccurredOn),
            cancellationToken);
    }
}
