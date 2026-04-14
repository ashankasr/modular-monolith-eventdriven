using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Domain.Errors;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.CancelOrder;

public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork) : ICommandHandler<CancelOrderCommand, CancelOrderResponse>
{
    public async Task<Result<CancelOrderResponse>> Handle(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<CancelOrderResponse>(OrderErrors.NotFound(command.OrderId));

        // CHOREOGRAPHY: MarkAsCancelled raises OrderCancelledDomainEvent, which the outbox
        // picks up during SaveChangesAsync and dispatches via OrderCancelledDomainEventHandler
        order.MarkAsCancelled(command.Reason);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CancelOrderResponse(order.Id);
    }
}
