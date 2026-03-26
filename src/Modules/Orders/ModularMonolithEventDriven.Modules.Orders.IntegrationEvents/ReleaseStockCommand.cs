namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record ReleaseStockCommand(
    Guid CorrelationId,
    Guid OrderId,
    Guid ReservationId);
