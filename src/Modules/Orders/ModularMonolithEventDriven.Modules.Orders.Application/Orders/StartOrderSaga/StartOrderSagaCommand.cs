using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.StartOrderSaga;

public sealed record StartOrderSagaCommand(
    string CustomerId,
    string CustomerEmail,
    List<StartOrderSagaItemDto> Items,
    bool SimulatePaymentFailure = false,
    bool SimulateStockFailure = false) : ICommand<StartOrderSagaResponse>;

public sealed record StartOrderSagaItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record StartOrderSagaResponse(Guid OrderId, Guid CorrelationId);
