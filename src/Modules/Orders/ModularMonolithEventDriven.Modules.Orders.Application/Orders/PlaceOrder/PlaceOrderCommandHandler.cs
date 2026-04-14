using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork) : ICommandHandler<PlaceOrderCommand, PlaceOrderResponse>
{
    public async Task<Result<PlaceOrderResponse>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();

        var items = command.Items
            .Select(i => new OrderItem(Guid.NewGuid(), orderId, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var orderResult = Order.Create(orderId, command.CustomerId, command.CustomerEmail, items);
        if (orderResult.IsFailure)
            return Result.Failure<PlaceOrderResponse>(orderResult.Error);

        orderRepository.Add(orderResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResponse(orderId, CorrelationId: orderId);
    }
}
