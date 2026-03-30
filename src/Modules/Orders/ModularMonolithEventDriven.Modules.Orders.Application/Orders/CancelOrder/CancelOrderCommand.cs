using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders.CancelOrder;

public sealed record CancelOrderCommand(Guid OrderId, string Reason) : ICommand<CancelOrderResponse>;

public sealed record CancelOrderResponse(Guid OrderId);
