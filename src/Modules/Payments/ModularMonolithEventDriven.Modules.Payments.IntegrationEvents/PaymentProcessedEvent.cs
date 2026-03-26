namespace ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

public sealed record PaymentProcessedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    DateTime ProcessedAt);
