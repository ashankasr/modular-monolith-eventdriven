using MassTransit;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Domain.Errors;
using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : ICommandHandler<CancelOrderCommand, CancelOrderResponse>
{
    public async Task<Result<CancelOrderResponse>> Handle(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<CancelOrderResponse>(OrderErrors.NotFound(command.OrderId));

        order.MarkAsCancelled();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // CHOREOGRAPHY: publish event — each module reacts independently
        await publishEndpoint.Publish(
            new OrderCancelledEvent(order.Id, command.Reason, DateTime.UtcNow),
            cancellationToken);

        return new CancelOrderResponse(order.Id);
    }
}
