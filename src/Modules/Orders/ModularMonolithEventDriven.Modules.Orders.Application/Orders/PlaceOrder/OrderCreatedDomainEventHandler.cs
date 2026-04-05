using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Domain.Events;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

/// <summary>
/// Reacts to OrderCreatedDomainEvent dispatched by the Outbox processor.
///
/// Flow:
///   OutboxMessageProcessor deserialises OutboxMessage
///     → MediatR.Publish(DomainEventNotification&lt;OrderCreatedDomainEvent&gt;)
///       → this handler
///         → IEventBus.PublishAsync(OrderCreatedIntegrationEvent)
///           → RabbitMQ → other modules (choreography)
///
/// Note: the Saga (orchestration) is still started directly in PlaceOrderCommandHandler.
/// This handler demonstrates the outbox-driven choreography path for the same creation fact.
/// </summary>
public sealed class OrderCreatedDomainEventHandler(
    IOrderRepository orderRepository,
    IEventBus eventBus) : IDomainEventHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<OrderCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null)
            return;

        await eventBus.PublishAsync(new OrderCreatedIntegrationEvent(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.TotalAmount,
            domainEvent.OccurredOn), cancellationToken);
    }
}
