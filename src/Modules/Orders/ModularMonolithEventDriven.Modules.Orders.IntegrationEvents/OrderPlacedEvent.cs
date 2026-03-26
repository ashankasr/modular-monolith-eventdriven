namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record OrderPlacedEvent(
    Guid OrderId,
    string CustomerId,
    string CustomerEmail,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    DateTime PlacedAt);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
