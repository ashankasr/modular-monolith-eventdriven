using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ModularMonolithEventDriven.Modules.Inventory.Application.Products.CreateProduct;
using ModularMonolithEventDriven.Modules.Inventory.Application.Products.GetProducts;

namespace ModularMonolithEventDriven.Modules.Inventory.Presentation;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory").WithTags("Inventory");

        group.MapGet("/products", async (ISender sender) =>
        {
            var result = await sender.Send(new GetProductsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithSummary("Get all products");

        group.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateProductCommand(request.Name, request.Sku, request.StockQuantity, request.Price));
            return result.IsSuccess
                ? Results.Created($"/api/inventory/products/{result.Value}", new { Id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Create a product (seed inventory)");

        return app;
    }
}

public sealed record CreateProductRequest(string Name, string Sku, int StockQuantity, decimal Price);
