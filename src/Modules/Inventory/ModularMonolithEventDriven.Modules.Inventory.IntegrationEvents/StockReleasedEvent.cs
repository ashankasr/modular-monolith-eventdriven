namespace ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;

public sealed record StockReleasedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid ReservationId,
    DateTime ReleasedAt);
