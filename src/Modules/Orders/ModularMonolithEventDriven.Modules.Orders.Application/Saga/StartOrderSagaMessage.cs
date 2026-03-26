using ModularMonolithEventDriven.Modules.Orders.IntegrationEvents;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Saga;

public sealed record StartOrderSagaMessage(
    Guid CorrelationId,
    Guid OrderId,
    string CustomerId,
    string CustomerEmail,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    bool SimulatePaymentFailure,
    bool SimulateStockFailure);
