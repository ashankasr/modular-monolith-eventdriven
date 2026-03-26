namespace ModularMonolithEventDriven.Modules.Inventory.IntegrationEvents;

public sealed record StockReservationFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason,
    DateTime FailedAt);
