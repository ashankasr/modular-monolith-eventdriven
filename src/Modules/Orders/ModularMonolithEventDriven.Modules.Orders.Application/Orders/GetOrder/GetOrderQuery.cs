using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

public sealed record OrderResponse(
    Guid Id,
    string CustomerId,
    string CustomerEmail,
    string Status,
    decimal TotalAmount,
    string? FailureReason,
    List<OrderItemResponse> Items,
    DateTime CreatedAt);

public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
