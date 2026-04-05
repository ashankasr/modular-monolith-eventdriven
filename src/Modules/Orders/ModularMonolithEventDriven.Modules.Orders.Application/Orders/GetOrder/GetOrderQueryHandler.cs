using Mapster;
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

        return order.Adapt<OrderResponse>();
    }
}
