using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Inventory.Domain.Events;
using ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.StockReservations;

public sealed class StockReservedDomainEventHandler(IBus bus)
    : IDomainEventHandler<StockReservedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<StockReservedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;
        await bus.Publish(
            new StockReservedEvent(e.CorrelationId, e.OrderId, e.ReservationId, e.OccurredOn),
            cancellationToken);
    }
}
