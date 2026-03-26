namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record ReserveStockCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemDto> Items);
