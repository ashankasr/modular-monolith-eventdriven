using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.GetOrder;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.StartOrderSaga;

namespace ModularMonolithEventDriven.Modules.Orders.Presentation;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        // CHOREOGRAPHY: Place order via event-driven choreography
        group.MapPost("/choreography", async (PlaceOrderRequest request, ISender sender) =>
        {
            var command = new PlaceOrderCommand(
                request.CustomerId,
                request.CustomerEmail,
                request.Items.Select(i => new PlaceOrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
                request.SimulatePaymentFailure,
                request.SimulateStockFailure);

            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { result.Value.OrderId, Pattern = "Choreography", Message = "Order placed. Watch the logs to see each module react to events." })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Place order (Choreography pattern)")
        .WithDescription("Places an order using the Choreography pattern. Modules communicate via events without a central coordinator.");

        // ORCHESTRATION: Place order via Saga state machine
        group.MapPost("/orchestration", async (PlaceOrderRequest request, ISender sender) =>
        {
            var command = new StartOrderSagaCommand(
                request.CustomerId,
                request.CustomerEmail,
                request.Items.Select(i => new StartOrderSagaItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
                request.SimulatePaymentFailure,
                request.SimulateStockFailure);

            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { result.Value.OrderId, result.Value.CorrelationId, Pattern = "Orchestration", Message = "Saga started. The saga orchestrates each step." })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Place order (Orchestration/Saga pattern)")
        .WithDescription("Places an order using the Orchestration pattern. A MassTransit Saga state machine coordinates each step.");

        // GET order status
        group.MapGet("/{orderId:guid}", async (Guid orderId, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderQuery(orderId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithSummary("Get order by ID");

        return app;
    }
}

public sealed record PlaceOrderRequest(
    string CustomerId,
    string CustomerEmail,
    List<PlaceOrderItemRequest> Items,
    bool SimulatePaymentFailure = false,
    bool SimulateStockFailure = false);

public sealed record PlaceOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);
