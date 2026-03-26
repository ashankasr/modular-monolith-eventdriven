namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record ProcessPaymentCommand(
    Guid CorrelationId,
    Guid OrderId,
    string CustomerId,
    string CustomerEmail,
    decimal Amount);
