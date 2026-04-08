namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed class SendOrderNotificationCommand
{
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
