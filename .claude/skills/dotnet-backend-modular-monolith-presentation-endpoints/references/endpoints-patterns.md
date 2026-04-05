# Presentation Layer — Minimal API Endpoints — Complete Reference

---

## 1. File Structure

```
Modules/<Name>/ModularMonolithEventDriven.Modules.<Name>.Presentation/
  <Module>Endpoints.cs     ← all routes for this module + request records
  AssemblyReference.cs
```

One file per module. All routes, request records, and the `IEndpointRouteBuilder` extension live here together.

---

## 2. Endpoint File Skeleton

```csharp
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
        var group = app.MapGroup("/api/inventory").WithTags(Tags.Inventory);

        // GET — query, returns list
        group.MapGet("/products", async (ISender sender) =>
        {
            var result = await sender.Send(new GetProductsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithSummary("Get all products");

        // POST — command, returns Created with location
        group.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
        {
            var result = await sender.Send(new CreateProductCommand(
                request.Name, request.Sku, request.StockQuantity, request.Price));
            return result.IsSuccess
                ? Results.Created($"/api/inventory/products/{result.Value}", new { Id = result.Value })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Create a product");

        return app;
    }
}

// Request records — co-located at the bottom of the same file
public sealed record CreateProductRequest(string Name, string Sku, int StockQuantity, decimal Price);
```

---

## 3. Result → IResult Mapping

Always check `result.IsSuccess` and return the appropriate HTTP result. Never throw from endpoints.

### Query (GET) — found / not found

```csharp
group.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
{
    var result = await sender.Send(new GetOrderQuery(id));
    return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
})
.WithSummary("Get order by ID");
```

### Command (POST) — created

```csharp
group.MapPost("/", async (CreateProductRequest request, ISender sender) =>
{
    var result = await sender.Send(new CreateProductCommand(
        request.Name, request.Sku, request.StockQuantity, request.Price));
    return result.IsSuccess
        ? Results.Created($"/api/inventory/products/{result.Value}", new { Id = result.Value })
        : Results.BadRequest(result.Error);
});
```

### Command (POST) — action with response body

```csharp
group.MapPost("/", async (PlaceOrderRequest request, ISender sender) =>
{
    var command = new PlaceOrderCommand(
        request.CustomerId,
        request.CustomerEmail,
        request.Items.Select(i => new PlaceOrderItemDto(
            i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList());

    var result = await sender.Send(command);
    return result.IsSuccess
        ? Results.Ok(result.Value)         // Result<PlaceOrderResponse> — return the response DTO
        : Results.BadRequest(result.Error);
});
```

### Command with path parameter (POST/PUT)

```csharp
group.MapPost("/{orderId:guid}/cancel", async (Guid orderId, CancelOrderRequest request, ISender sender) =>
{
    var result = await sender.Send(new CancelOrderCommand(orderId, request.Reason));
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Error);
})
.WithSummary("Cancel order");
```

### Result mapping quick reference

| Scenario | HTTP result |
|---|---|
| Query found | `Results.Ok(result.Value)` |
| Query not found | `Results.NotFound(result.Error)` |
| Command creates a resource | `Results.Created("/path/{id}", new { Id = result.Value })` |
| Command succeeds with response | `Results.Ok(result.Value)` |
| Command succeeds, no body needed | `Results.NoContent()` |
| Validation or domain failure | `Results.BadRequest(result.Error)` |

---

## 4. Route Grouping and OpenAPI Metadata

```csharp
// Group all routes under a shared prefix + tag
var group = app.MapGroup("/api/orders").WithTags(Tags.Orders);

// Per-route metadata
group.MapPost("/", ...)
    .WithSummary("Place order (Orchestration/Saga pattern)")
    .WithDescription("Places an order using the Orchestration pattern. A MassTransit Saga state machine coordinates each step.");

group.MapGet("/{orderId:guid}", ...)
    .WithSummary("Get order by ID");
```

`Tags` is a static class defined in the `Presentation` project:

```csharp
// Tags.cs — add once per project
internal static class Tags
{
    internal const string Orders = "Orders";
    internal const string Inventory = "Inventory";
    // etc.
}
```

---

## 5. Request Records

Co-locate request records at the bottom of the endpoints file — no separate file needed.

```csharp
// Simple flat request
public sealed record CreateProductRequest(string Name, string Sku, int StockQuantity, decimal Price);

// Request with nested collection
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

// Request with optional fields
public sealed record CancelOrderRequest(string Reason = "Cancelled by customer");
```

**Rules:**
- Always `sealed record`
- No domain objects — only primitives, `Guid`, `decimal`, `List<T>` of other records
- Map to the command/query DTO inside the endpoint lambda (not inside the record)

---

## 6. Registering Endpoints in the API Host

### In `WebApplicationExtensions.cs` (API host)

```csharp
// src/Api/ModularMonolithEventDriven.Api/Extensions/WebApplicationExtensions.cs
internal static class WebApplicationExtensions
{
    internal static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapOrdersEndpoints();
        app.MapInventoryEndpoints();
        app.MapPaymentsEndpoints();
        app.MapNotificationsEndpoints();
        app.MapShippingEndpoints();   // ← add new module here
        return app;
    }
}
```

### The API host `.csproj` must reference the module's `Presentation` project

```xml
<!-- ModularMonolithEventDriven.Api.csproj -->
<ProjectReference Include="..\..\Modules\Shipping\
    ModularMonolithEventDriven.Modules.Shipping.Presentation\
    ModularMonolithEventDriven.Modules.Shipping.Presentation.csproj" />
```

---

## 7. Presentation Project Dependencies

```xml
<!-- <Module>.Presentation.csproj -->
<ItemGroup>
  <!-- Only depends on Application — never on Infrastructure or Domain directly -->
  <ProjectReference Include="..\ModularMonolithEventDriven.Modules.<Name>.Application\..." />
</ItemGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
</ItemGroup>
```

**Allowed dependencies:**
- Own module's `Application` project ✅ (for commands/queries)
- `Microsoft.AspNetCore.OpenApi` ✅
- Own module's `Domain` ❌ — never reference directly; use the Application DTOs
- `Infrastructure` ❌ — never

---

## 8. Checklist — Adding a New Endpoint

- [ ] Open the module's `<Module>Endpoints.cs`
- [ ] Add a request record at the bottom (if the endpoint takes a body)
- [ ] Add the route inside `Map<Module>Endpoints()` under the existing group
- [ ] Map the request record fields to the correct Command/Query constructor
- [ ] Call `sender.Send(command)` and check `result.IsSuccess`
- [ ] Return the correct `IResult` (see mapping table in §3)
- [ ] Add `.WithSummary(...)` for Scalar/OpenAPI docs
- [ ] If it's a new module, add `app.Map<Module>Endpoints()` to `WebApplicationExtensions.cs`
- [ ] If it's a new module, add `ProjectReference` to the API host `.csproj`
