namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed class ReleaseStockCommand
{
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ReservationId { get; set; }
}
