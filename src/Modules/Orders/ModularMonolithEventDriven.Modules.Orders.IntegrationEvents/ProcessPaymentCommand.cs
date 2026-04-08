namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed class ProcessPaymentCommand
{
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
