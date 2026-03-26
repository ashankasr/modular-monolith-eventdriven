namespace ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;

public sealed record StockReservedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid ReservationId,
    DateTime ReservedAt);
