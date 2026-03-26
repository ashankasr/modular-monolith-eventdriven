namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record SendOrderNotificationCommand(
    Guid CorrelationId,
    Guid OrderId,
    string CustomerEmail,
    string Status,
    string Message);
