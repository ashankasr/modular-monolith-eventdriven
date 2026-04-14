# Full-Pipeline Feature Implementation — Complete Reference

This document is the master guide for implementing a feature end-to-end in this .NET 9 Modular Monolith.  
Work through the phases in order. Each phase produces artefacts that gate entry into the next phase.

---

## Phase 1 — Brainstorm: Define the Requirement

Before any code is written, answer every question in this section. Write the answers down and confirm with the user. Ambiguity here creates broken wiring between layers.

### 1.1 The Operation

| Question | Why it matters |
|---|---|
| What action does the user/system want to take? | Determines command name, domain method name, and HTTP verb |
| What is the primary domain entity affected? | Determines which module owns this feature |
| Is this a **write** (state change) or a **read** (data retrieval)? | Drives Command vs Query, endpoint verb, UoW usage |
| What data does the caller supply? | Shapes the HTTP request body / path params and the Command/Query record |
| What data does the caller expect back? | Shapes the response DTO and the HTTP status code |

### 1.2 The Domain Impact

| Question | Why it matters |
|---|---|
| Does this change entity state in the DB? | Whether a `UnitOfWork.SaveChangesAsync()` is needed |
| Does this create, update, or delete an aggregate? | Whether a repository `Add/Update/Remove` is called |
| Are any **invariants** or **business rules** enforced? | Whether the entity method returns `Result<T>` or throws |
| What are the **failure scenarios** and their error messages? | Defines the `Error` constants on the entity |
| Does this affect a **new entity** that doesn't exist yet? | Triggers the domain-ef skill first |

### 1.3 The Event Story

Answer these to determine whether integration events or a saga are involved:

| Question | Answer options |
|---|---|
| Does this operation need to **notify another module**? | Yes → integration event; No → stays within this module |
| Is notification **fire-and-forget** (no reply needed)? | Yes → choreography consumer in target module |
| Does this operation need to **coordinate with multiple modules** and handle failures? | Yes → saga orchestration step |
| Is this the **trigger that starts** a saga? | Yes → the command handler publishes the saga's start event |
| Is this a **step within an existing saga**? | Yes → a new saga transition + new consumer in the target module |
| Should the integration event be published **atomically with the DB write**? | Yes (default) → raise a domain event → outbox → IDomainEventHandler publishes to broker |

### 1.4 The HTTP Contract

Write this out explicitly before proceeding:

```
Verb:     POST / GET / PUT / DELETE / PATCH
Route:    /api/<module>/<resource>[/{id}]
Request:  { field1: type, field2: type, ... }  OR  path param only  OR  query string
Response: 200 { ... } | 201 { id: guid } | 204  on success
Errors:   400 { error: "..." }  |  404  |  409
```

### 1.5 The Validation Rules

List every validation rule for the incoming request:
- Required fields
- Length constraints
- Range constraints
- Business rule constraints (e.g. "quantity must be positive")

These map 1:1 to `FluentValidation` rules in the Application layer.

---

## Phase 2 — Pipeline Design: Decision Matrix

Use the brainstorm answers to fill this matrix. Each row that is "Yes" adds a layer to the implementation plan.

| Layer needed? | Condition | Artefacts produced |
|---|---|---|
| **New domain entity / value object** | Feature operates on an entity that doesn't exist yet | Entity class, EF config, migration |
| **New repository method** | Feature needs a query that doesn't exist in the repo interface | `IXxxRepository` method + `XxxRepository` implementation |
| **Command + Handler** | Write operation (state change) | `XxxCommand.cs`, `XxxCommandHandler.cs` |
| **Query + Handler** | Read operation | `XxxQuery.cs`, `XxxResponse.cs`, `XxxQueryHandler.cs` |
| **FluentValidation validator** | Any command with user-supplied input | `XxxCommandValidator.cs` |
| **Domain event** | State change that other handlers or the outbox need to react to | `XxxDomainEvent.cs` raised inside the entity method |
| **IDomainEventHandler** | Domain event must trigger an integration event or side-effect | `XxxDomainEventHandler.cs` in Infrastructure |
| **Integration event contract** | Another module (or the saga) needs to be notified | New record in `<Module>.IntegrationEvents` project |
| **New consumer in target module** | Another module must react to the event | `XxxConsumer.cs` in target module's `Infrastructure/Consumers/` |
| **Saga step** | Coordination with multiple modules + compensation needed | New saga state transition + command event + result event |
| **Presentation endpoint** | Feature must be callable over HTTP | New `MapPost/MapGet/...` in `<Module>Endpoints.cs` |

### Pattern selector: where does the integration event get published?

```
Command handler (save state)
  └─ Entity raises DomainEvent during state-change method
       └─ EF SaveChanges interceptor writes OutboxMessage row (same transaction)
            └─ OutboxMessageProcessor (background, Quartz, 30s poll)
                 └─ IDomainEventHandler<T> publishes MassTransit message to RabbitMQ
                      └─ Consumer in target module reacts
```

**Rule**: Never call `publishEndpoint.Publish(...)` directly from a command handler. Always go through domain event → outbox → IDomainEventHandler.

---

## Phase 3 — Implementation Order

Build in this sequence. Each layer depends on the one above it being stable.

```
1. Domain Layer
   a. Add/modify entity behaviour method (enforces invariants, raises domain event)
   b. Add domain event record if needed
   c. Add repository interface method if needed

2. Application Layer
   a. Command/Query record
   b. Response DTO (for queries or commands that return data)
   c. CommandHandler / QueryHandler
   d. FluentValidation validator (for commands)

3. Infrastructure Layer — Persistence
   a. EF entity configuration (if new entity or new property)
   b. Repository implementation (new query methods)
   c. EF migration (if schema changed)

4. Integration Events Layer (if applicable)
   a. Add integration event / saga command record to the owning module's IntegrationEvents project

5. Infrastructure Layer — Messaging (if applicable)
   a. IDomainEventHandler (publishes integration event from outbox)
   b. Consumer in target module
   c. Register consumer in target module's ConfigureConsumers

6. Presentation Layer
   a. Request record in <Module>Endpoints.cs
   b. MapPost/MapGet/... lambda
   c. Wire into WebApplicationExtensions.cs if brand new module
```

**Why this order?**  
Domain is pure C# — no dependencies. Application depends on Domain interfaces. Infrastructure implements those interfaces. Integration events are pure contracts so they can be defined once the Application handler shape is known. Presentation is last because it maps HTTP → Application command/query which must exist first.

---

## Phase 4 — Layer-by-Layer Implementation

### 4.1 Domain Layer

**Entity behaviour method pattern**

```csharp
// Domain/<Entity>/<Entity>.cs
public static Result<Product> Create(Guid id, string name, string sku, int stockQty, decimal price)
{
    if (string.IsNullOrWhiteSpace(name))
        return Result.Failure<Product>(ProductErrors.NameRequired);
    if (price <= 0)
        return Result.Failure<Product>(ProductErrors.InvalidPrice(price));

    var product = new Product { Id = id, Name = name, Sku = sku, StockQuantity = stockQty, Price = price };
    product.RaiseDomainEvent(new ProductCreatedDomainEvent(id, name, sku));   // if downstream reaction needed
    return product;
}
```

**Domain event record**

```csharp
// Domain/<Entity>/Events/<Action><Entity>DomainEvent.cs
public sealed record ProductCreatedDomainEvent(Guid ProductId, string Name, string Sku) : IDomainEvent;
```

**Error constants** (defined as static members on a sibling `<Entity>Errors` class)

```csharp
// Domain/<Entity>/<Entity>Errors.cs
public static class ProductErrors
{
    public static readonly Error NameRequired = Error.Validation("Product.NameRequired", "Name is required.");
    public static Error InvalidPrice(decimal p) => Error.Validation("Product.InvalidPrice", $"Price {p} is not valid.");
    public static Error NotFound(Guid id) => Error.NotFound("Product.NotFound", $"Product {id} was not found.");
}
```

**Repository interface addition**

```csharp
// Domain/<Entity>/I<Entity>Repository.cs  (add new method)
Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
```

> Detailed patterns: load `dotnet-backend-modular-monolith-domain-ef` skill references.

---

### 4.2 Application Layer

**Command record** (`Application/<Feature>/<Action><Entity>/`)

```csharp
public sealed record CreateProductCommand(string Name, string Sku, int StockQuantity, decimal Price)
    : ICommand<Guid>;
```

**Query record + response DTO**

```csharp
public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<ProductResponse>;

public sealed record ProductResponse(Guid Id, string Name, string Sku, int StockQuantity, decimal Price);
```

**Command handler with UoW**

```csharp
public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IInventoryUnitOfWork unitOfWork) : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var result = Product.Create(Guid.NewGuid(), command.Name, command.Sku, command.StockQuantity, command.Price);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        productRepository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.Id;
    }
}
```

**Query handler with Mapster**

```csharp
public sealed class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(query.ProductId, cancellationToken);
        if (product is null)
            return Result.Failure<ProductResponse>(ProductErrors.NotFound(query.ProductId));

        return product.Adapt<ProductResponse>();
    }
}
```

**FluentValidation validator**

```csharp
public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

> Detailed patterns: load `dotnet-backend-modular-monolith-cqrs-patterns` skill references.

---

### 4.3 Infrastructure Layer — Persistence

**EF entity configuration** (add inside `OnModelCreating` of the module's DbContext)

```csharp
builder.Entity<Product>(b =>
{
    b.ToTable("Products", "inventory");
    b.HasKey(p => p.Id);
    b.Property(p => p.Name).HasMaxLength(200).IsRequired();
    b.Property(p => p.Sku).HasMaxLength(50).IsRequired();
    b.HasIndex(p => p.Sku).IsUnique();
    b.Property(p => p.Price).HasPrecision(18, 2);
});
```

**Repository method implementation**

```csharp
public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    => await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
```

**EF Migration command** (run from the Infrastructure project directory)

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Modules/<Name>/Ochestrator.Modules.<Name>.Infrastructure \
  --startup-project src/Api/Ochestrator.Api
```

> Detailed patterns: load `dotnet-backend-modular-monolith-domain-ef` skill references.

---

### 4.4 Integration Events Layer (if applicable)

**Event contract** (in the owning module's `IntegrationEvents` project)

```csharp
// Modules/<Name>/Ochestrator.Modules.<Name>.IntegrationEvents/
public sealed record ProductCreatedIntegrationEvent(
    Guid ProductId,
    string Name,
    string Sku,
    DateTime OccurredAt);
```

**Saga command contract** (sent by saga TO a module — lives in the target module's IntegrationEvents)

```csharp
public sealed record ReserveStockCommand(
    Guid CorrelationId,
    Guid OrderId,
    List<OrderItemDto> Items);
```

**Result event** (published back to the saga after the command is handled)

```csharp
public sealed record StockReservedEvent(Guid CorrelationId, Guid OrderId, Guid ReservationId, DateTime OccurredAt);
public sealed record StockReservationFailedEvent(Guid CorrelationId, Guid OrderId, string Reason, DateTime OccurredAt);
```

> Detailed patterns: load `dotnet-backend-modular-monolith-integration-events-consumers` skill references.

---

### 4.5 Infrastructure Layer — Messaging (if applicable)

**IDomainEventHandler — outbox to broker bridge**

```csharp
// Infrastructure/DomainEventHandlers/ProductCreatedDomainEventHandler.cs
internal sealed class ProductCreatedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<ProductCreatedDomainEvent>
{
    public async Task Handle(DomainEventNotification<ProductCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        await publishEndpoint.Publish(
            new ProductCreatedIntegrationEvent(domainEvent.ProductId, domainEvent.Name, domainEvent.Sku, DateTime.UtcNow),
            cancellationToken);
    }
}
```

**Consumer in the reacting module** (choreography — autonomous reaction)

```csharp
// Infrastructure/Consumers/ProductCreatedConsumer.cs  (in Notifications module, for example)
internal sealed class ProductCreatedConsumer(ISender sender) : IConsumer<ProductCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<ProductCreatedIntegrationEvent> context)
    {
        await sender.Send(new NotifyProductCreatedCommand(
            context.Message.ProductId,
            context.Message.Name));
    }
}
```

**Consumer in the reacting module** (orchestration — saga command responder)

```csharp
internal sealed class ReserveStockCommandConsumer(ISender sender) : IConsumer<ReserveStockCommand>
{
    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var result = await sender.Send(
            new ReserveStockCommand(context.Message.OrderId, context.Message.Items));

        if (result.IsSuccess)
            await context.Publish(new StockReservedEvent(context.Message.CorrelationId, context.Message.OrderId,
                result.Value, DateTime.UtcNow));
        else
            await context.Publish(new StockReservationFailedEvent(context.Message.CorrelationId,
                context.Message.OrderId, result.Error.Description, DateTime.UtcNow));
    }
}
```

**Register consumer** (in the module's `ConfigureConsumers` method)

```csharp
public static void ConfigureConsumers(IRegistrationConfigurator configurator)
{
    configurator.AddConsumer<ProductCreatedConsumer>();       // existing or new line
    configurator.AddConsumer<ReserveStockCommandConsumer>();  // new line
}
```

> Detailed patterns: load `dotnet-backend-modular-monolith-integration-events-consumers` skill references.

---

### 4.6 Presentation Layer

**Request record + endpoint** (in `<Module>Endpoints.cs`)

```csharp
// Request record — defined at the top of the file alongside other records
public sealed record CreateProductRequest(string Name, string Sku, int StockQuantity, decimal Price);

// Inside MapInventoryEndpoints()
group.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
{
    var result = await sender.Send(
        new CreateProductCommand(request.Name, request.Sku, request.StockQuantity, request.Price));

    return result.IsSuccess
        ? Results.Created($"/api/inventory/products/{result.Value}", new { Id = result.Value })
        : Results.BadRequest(result.Error);
})
.WithSummary("Create a new product")
.WithDescription("Creates a product and adds it to the inventory catalogue.");
```

**Result → HTTP status mapping rules**

| Scenario | HTTP result |
|---|---|
| Command succeeds (created new resource) | `201 Created` with `Location` header and `{ id }` body |
| Command succeeds (updated/deleted) | `200 Ok` or `204 NoContent` |
| Query succeeds | `200 Ok` with response DTO |
| `Result.IsFailure` with `Error.NotFound` | `404 NotFound` |
| `Result.IsFailure` with `Error.Conflict` | `409 Conflict` |
| `Result.IsFailure` with `Error.Validation` | `400 BadRequest` |
| FluentValidation failure (pipeline behavior) | `400 BadRequest` (auto, no endpoint code needed) |

> Detailed patterns: load `dotnet-backend-modular-monolith-presentation-endpoints` skill references.

---

## Phase 5 — Verification Checklist

Work through every item before marking the feature done.

### Build & structure
- [ ] `dotnet build` — 0 errors, 0 warnings
- [ ] All new files are in the correct layer (Domain/Application/Infrastructure/Presentation)
- [ ] No cross-module project references (only IntegrationEvents contracts shared)

### Domain
- [ ] Entity method returns `Result<T>` and raises domain event if applicable
- [ ] All error codes are defined in `<Entity>Errors` static class
- [ ] Repository interface has any new query methods

### Application
- [ ] Command or Query record is `sealed record`
- [ ] Handler uses `ICommand<T>` or `IQuery<T>` via the correct interface
- [ ] Validator added for every command with user-supplied input
- [ ] Response DTO uses `Adapt<T>()` where mapping is needed

### Infrastructure — Persistence
- [ ] EF entity configuration is inside `OnModelCreating` (not via annotations)
- [ ] New `DbSet<T>` added to the module DbContext
- [ ] EF migration created (`dotnet ef migrations add ...`)
- [ ] Migration reviewed — no unintended table/column drops

### Infrastructure — Messaging (if applicable)
- [ ] `IDomainEventHandler` publishes to RabbitMQ — NOT the command handler directly
- [ ] Consumer is registered in `ConfigureConsumers`
- [ ] Integration event contract is in the correct module's `IntegrationEvents` project
- [ ] Cross-module `.csproj` reference for the IntegrationEvents contract project is added

### Presentation
- [ ] Request record is `sealed record`
- [ ] All 4xx paths are handled (`NotFound`, `BadRequest`)
- [ ] `.WithSummary()` added so Scalar UI shows a useful label
- [ ] `MapXxxEndpoints()` is called from `WebApplicationExtensions.cs`

### Runtime smoke test
- [ ] App starts without exceptions
- [ ] RabbitMQ queues created (visible at http://localhost:15672)
- [ ] Happy-path request returns expected status code and body
- [ ] Failure-path request (e.g. bad input) returns 400 with `{ code, description }`
- [ ] If integration event: consuming module receives the message (check logs)

---

## Quick Reference: Which Specialist Skill to Load Next

| Task | Skill to load |
|---|---|
| Add/modify entity, EF config, migration | `dotnet-backend-modular-monolith-domain-ef` |
| Add command/query/handler/validator | `dotnet-backend-modular-monolith-cqrs-patterns` |
| Add integration event contract or consumer | `dotnet-backend-modular-monolith-integration-events-consumers` |
| Add/modify a saga transition | `dotnet-backend-modular-monolith-saga` |
| Add HTTP endpoint | `dotnet-backend-modular-monolith-presentation-endpoints` |
| Create an entirely new module | `dotnet-backend-modular-monolith-eventdriven-create-module` |
| Architecture or pattern questions | `dotnet-backend-modular-monolith-eventdriven-architecture` |
