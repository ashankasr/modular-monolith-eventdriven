using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Domain.Errors;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.GetOrder;

public sealed class GetOrderQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(
        GetOrderQuery query,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure<OrderResponse>(OrderErrors.NotFound(query.OrderId));

        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.Status.ToString(),
            order.TotalAmount,
            order.FailureReason,
            order.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            order.CreatedAt);
    }
}
