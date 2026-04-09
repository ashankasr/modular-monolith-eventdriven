using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

public sealed record PlaceOrderCommand(
    string CustomerId,
    string CustomerEmail,
    List<PlaceOrderItemDto> Items) : ICommand<PlaceOrderResponse>;

public sealed record PlaceOrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record PlaceOrderResponse(Guid OrderId, Guid CorrelationId);
