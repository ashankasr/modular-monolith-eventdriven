using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.CancelOrder;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.GetOrder;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.PlaceOrder;

namespace ModularMonolithEventDriven.Modules.Orders.Presentation;

public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags(Tags.Orders);

        // ORCHESTRATION: Place order via Saga state machine
        group.MapPost("/", async (PlaceOrderRequest request, ISender sender) =>
        {
            var command = new PlaceOrderCommand(
                request.CustomerId,
                request.CustomerEmail,
                [.. request.Items.Select(i => new PlaceOrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))]);

            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { result.Value.OrderId, result.Value.CorrelationId, Pattern = "Orchestration", Message = "Saga started. The saga coordinates: ReserveStock → ProcessPayment → Notify, with compensation on failure." })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Place order (Orchestration/Saga pattern)")
        .WithDescription("Places an order using the Orchestration pattern. A MassTransit Saga state machine coordinates each step and handles compensation on failure.");

        // CHOREOGRAPHY: Cancel order — each module reacts to OrderCancelledEvent independently
        group.MapPost("/{orderId:guid}/cancel", async (Guid orderId, CancelOrderRequest request, ISender sender) =>
        {
            var command = new CancelOrderCommand(orderId, request.Reason);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { result.Value.OrderId, Pattern = "Choreography", Message = "OrderCancelledEvent published. Watch the logs: Inventory releases stock, Payments refunds, Notifications confirms — each module reacts independently." })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Cancel order (Choreography pattern)")
        .WithDescription("Publishes an OrderCancelledEvent. Each module reacts autonomously with no central coordinator: Inventory releases reserved stock, Payments issues a refund, Notifications sends a cancellation confirmation.");

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
    List<PlaceOrderItemRequest> Items);

public sealed record PlaceOrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public sealed record CancelOrderRequest(string Reason = "Cancelled by customer");
