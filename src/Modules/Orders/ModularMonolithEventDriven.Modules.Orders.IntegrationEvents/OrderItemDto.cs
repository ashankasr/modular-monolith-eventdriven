namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
