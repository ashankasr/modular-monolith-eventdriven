using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : ICommandHandler<PlaceOrderCommand, PlaceOrderResponse>
{
    public async Task<Result<PlaceOrderResponse>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var items = command.Items
            .Select(i => new OrderItem(Guid.NewGuid(), orderId, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = Order.Create(orderId, command.CustomerId, command.CustomerEmail, items);
        orderRepository.Add(order);

        // Publish integration event for CHOREOGRAPHY pattern
        var integrationEvent = new OrderPlacedEvent(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            order.TotalAmount,
            DateTime.UtcNow);

        await publishEndpoint.Publish(integrationEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResponse(orderId);
    }
}
