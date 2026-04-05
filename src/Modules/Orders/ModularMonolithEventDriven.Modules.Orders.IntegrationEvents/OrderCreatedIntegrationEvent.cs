namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

/// <summary>
/// Published to RabbitMQ after an order is created.
/// Other modules can subscribe to react independently (choreography).
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    string CustomerId,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime OccurredOnUtc);
