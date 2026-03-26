namespace ModularMonolithEventDriven.Modules.Payments.IntegrationEvents;

public sealed record PaymentFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason,
    DateTime FailedAt);
