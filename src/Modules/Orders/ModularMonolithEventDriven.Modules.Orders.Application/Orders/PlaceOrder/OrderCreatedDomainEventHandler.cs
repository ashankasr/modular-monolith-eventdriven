using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Domain.Events;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

public sealed class OrderCreatedDomainEventHandler(
    IOrderRepository orderRepository,
    IEventBus eventBus) : IDomainEventHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<OrderCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(notification.DomainEvent.OrderId, cancellationToken);
        if (order is null)
            return;

        await eventBus.PublishAsync(new OrderSagaStartMessage(
            CorrelationId: order.Id,
            OrderId: order.Id,
            order.CustomerId,
            order.CustomerEmail,
            [.. order.Items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))],
            order.TotalAmount), cancellationToken);
    }
}
