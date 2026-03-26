using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.StartOrderSaga;

public sealed class StartOrderSagaCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : ICommandHandler<StartOrderSagaCommand, StartOrderSagaResponse>
{
    public async Task<Result<StartOrderSagaResponse>> Handle(
        StartOrderSagaCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var items = command.Items
            .Select(i => new OrderItem(Guid.NewGuid(), orderId, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = Order.Create(orderId, command.CustomerId, command.CustomerEmail, items);
        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Send message to start the saga
        await publishEndpoint.Publish(new StartOrderSagaMessage(
            correlationId,
            orderId,
            command.CustomerId,
            command.CustomerEmail,
            items.Select(i => new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            order.TotalAmount,
            command.SimulatePaymentFailure,
            command.SimulateStockFailure), cancellationToken);

        return new StartOrderSagaResponse(orderId, correlationId);
    }
}
