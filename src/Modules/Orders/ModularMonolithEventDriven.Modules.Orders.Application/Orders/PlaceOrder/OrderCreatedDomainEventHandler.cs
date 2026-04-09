using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Domain.Events;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

public sealed class OrderCreatedDomainEventHandler(
    IOrderRepository orderRepository) : IDomainEventHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<OrderCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null)
            return;
    }
}
