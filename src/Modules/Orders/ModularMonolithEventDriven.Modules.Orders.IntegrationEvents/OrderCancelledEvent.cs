namespace ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

public sealed record OrderCancelledEvent(
    Guid OrderId,
    string Reason,
    DateTime CancelledAt);
