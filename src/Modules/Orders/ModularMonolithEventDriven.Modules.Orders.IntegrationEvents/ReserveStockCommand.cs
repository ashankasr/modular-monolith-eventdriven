namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed class ReserveStockCommand
{
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
}
