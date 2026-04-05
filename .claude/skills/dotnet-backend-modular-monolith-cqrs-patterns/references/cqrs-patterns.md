# CQRS Patterns — Complete Reference

---

## 1. CQRS Type Hierarchy

### Interfaces (from `Common.Application.Abstractions`)

```
ICommand                           → write operation, no return value  → IRequest<Result>
ICommand<TResponse>                → write operation, returns data      → IRequest<Result<TResponse>>
IQuery<TResponse>                  → read operation, always returns     → IRequest<Result<TResponse>>

ICommandHandler<TCommand>          → handles ICommand
ICommandHandler<TCommand, TRes>    → handles ICommand<TRes>
IQueryHandler<TQuery, TRes>        → handles IQuery<TRes>
```

**Rule:** Commands mutate state. Queries only read. Never mix.

---

## 2. Command — Definition

File: `Application/<Feature>/<Action><Entity>/<Action><Entity>Command.cs`

```csharp
// Write op with no meaningful return
public sealed record DeleteProductCommand(Guid ProductId) : ICommand;

// Write op that returns data (e.g. the new entity's Id)
public sealed record CreateProductCommand(
    string Name,
    string Sku,
    int StockQuantity,
    decimal Price) : ICommand<Guid>;
```

- Always `sealed record`
- Positional constructor parameters = the input data
- No logic — pure data carrier

---

## 3. CommandHandler — Definition

File: `Application/<Feature>/<Action><Entity>/<Action><Entity>CommandHandler.cs`

### With UnitOfWork (state change persisted to DB)
```csharp
public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IInventoryUnitOfWork unitOfWork) : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var productResult = Product.Create(Guid.NewGuid(), command.Name, command.Sku, command.StockQuantity, command.Price);
        if (productResult.IsFailure)
            return Result.Failure<Guid>(productResult.Error);

        var product = productResult.Value;
        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;   // implicit conversion: TValue → Result<TValue>
    }
}
```

### With failure path
```csharp
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork) : ICommandHandler<CancelOrderCommand, CancelOrderResponse>
{
    public async Task<Result<CancelOrderResponse>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<CancelOrderResponse>(OrderErrors.NotFound(command.OrderId));

        order.MarkAsCancelled();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CancelOrderResponse(order.Id);
    }
}
```

### With Integration Event (cross-module side effect)
```csharp
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork,
    IPublishEndpoint publishEndpoint) : ICommandHandler<CancelOrderCommand, CancelOrderResponse>
{
    public async Task<Result<CancelOrderResponse>> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(command.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<CancelOrderResponse>(OrderErrors.NotFound(command.OrderId));

        order.MarkAsCancelled();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish integration event for other modules to react to
        await publishEndpoint.Publish(new OrderCancelledEvent(order.Id, command.Reason, DateTime.UtcNow), cancellationToken);

        return new CancelOrderResponse(order.Id);
    }
}
```

**Checklist:**
- [ ] `ICommandHandler` not `IRequestHandler`
- [ ] Always `sealed class`
- [ ] Inject only the module's own `I<Module>UnitOfWork`, never another module's
- [ ] Call `unitOfWork.SaveChangesAsync()` before publishing integration events
- [ ] Use `Result.Failure<T>(error)` for failure, implicit `return value` for success

---

## 4. Query — Definition

File: `Application/<Feature>/Get<Entity>/<Get...>Query.cs`

```csharp
// Query with no parameters
public sealed record GetProductsQuery : IQuery<List<ProductDto>>;

// Query with parameters
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

// Response DTOs — co-located in the same file
public sealed record ProductDto(Guid Id, string Name, string Sku, int StockQuantity, decimal Price);

public sealed record OrderResponse(
    Guid Id,
    string CustomerId,
    string Status,
    decimal TotalAmount,
    List<OrderItemResponse> Items,
    DateTime CreatedAt);

public sealed record OrderItemResponse(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);
```

- Always `sealed record`
- Response DTOs defined in the same file as the query
- DTOs are flat — no domain objects leak out

---

## 5. QueryHandler — Definition

File: `Application/<Feature>/Get<Entity>/<Get...>QueryHandler.cs`

### Simple — manual projection
```csharp
public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.StockQuantity, p.Price)).ToList();
    }
}
```

### With Mapster (complex entities / nested types)
```csharp
public sealed class GetOrderQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);
        if (order is null)
            return Result.Failure<OrderResponse>(OrderErrors.NotFound(query.OrderId));

        return order.Adapt<OrderResponse>();   // Mapster — config registered at startup
    }
}
```

---

## 6. Result / Error Pattern

### Result factories
```csharp
Result.Success()                     // void command success
Result.Failure(error)                // void command failure
Result.Success<T>(value)             // explicit success with value
Result.Failure<T>(error)             // typed failure
return value;                        // implicit conversion TValue → Result<TValue>
```

### Error factories (`Common.Domain.Results.Error`)
```csharp
Error.NotFound("Module.EntityNotFound", $"Entity '{id}' not found.")
Error.Validation("Module.InvalidInput", "...")
Error.Conflict("Module.AlreadyExists", "...")
Error.Failure("Module.OperationFailed", "...")
```

### Error class per module (`Domain/Errors/<Module>Errors.cs`)
```csharp
public static class InventoryErrors
{
    public static Error ProductNotFound(Guid id) =>
        Error.NotFound("Inventory.ProductNotFound", $"Product '{id}' not found.");

    public static Error InsufficientStock(string productName) =>
        Error.Failure("Inventory.InsufficientStock", $"Insufficient stock for '{productName}'.");
}
```

**Convention:** Error codes are `<Module>.<PascalCaseDescription>`.

---

## 7. DDD Entity Types — Decision Guide

### Base class hierarchy
```
Entity<TKey>                 → identity + domain events (no timestamps)
  └── AuditableEntity<TKey>  → + CreatedAt, UpdatedAt (auto-set by BaseDbContext)
        └── AuditableGuidEntity  → shorthand for AuditableEntity<Guid>
```

### When to use which
| Use | Base class |
|---|---|
| Entity needs `Guid` Id + timestamps | `AuditableGuidEntity` ← **default for aggregate roots** |
| Entity needs a non-Guid key | `AuditableEntity<TKey>` |
| Child entity (value object-ish, no timestamps needed) | `Entity<Guid>` |

### Entity pattern
```csharp
public sealed class Product : AuditableGuidEntity
{
    private Product() { }  // ← required for EF Core

    private Product(Guid id, string name, string sku, int stockQuantity, decimal price) : base(id)
    {
        Name = name;
        Sku = sku;
        StockQuantity = stockQuantity;
        Price = price;
    }

    // Properties — private setters, only mutated via methods
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int StockQuantity { get; private set; }
    public decimal Price { get; private set; }

    // Factory method — returns Result<T>, never throws
    public static Result<Product> Create(Guid id, string name, string sku, int stockQuantity, decimal price)
    {
        if (string.IsNullOrEmpty(name))
            return Result.Failure<Product>(Error.Validation("Product.InvalidName", "Name cannot be empty."));
        if (string.IsNullOrEmpty(sku))
            return Result.Failure<Product>(Error.Validation("Product.InvalidSku", "SKU cannot be empty."));
        return new Product(id, name, sku, stockQuantity, price);
    }

    // Domain behaviour — encapsulates business rules
    public void ReserveStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock for product {Name}.");
        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}
```

**Rules:**
- Always `sealed class`
- Private constructor + `private Entity() { }` for EF
- All setters `private set`
- `static Create(...)` factory validates inputs before construction
- Behaviour methods express business language (not `SetStatus()` → `MarkAsCancelled()`)

---

## 8. Domain Events

Raised within the entity when something meaningful happens. Used for intra-module reactions.

### Define (`Domain/Events/<EventName>DomainEvent.cs`)
```csharp
public sealed record OrderCreatedDomainEvent(
    Guid OrderId,
    string CustomerId) : DomainEvent;
// DomainEvent base provides: EventId (Guid), OccurredOn (DateTime)
```

### Raise (inside the entity)
```csharp
private Order(Guid id, string customerId, ...) : base(id)
{
    // ... set properties
    RaiseDomainEvent(new OrderCreatedDomainEvent(id, customerId));
}
```

Domain events accumulate on `entity.DomainEvents` (from `Entity<TKey>`). They are not dispatched automatically in this codebase — wire up a dispatcher in `BaseDbContext.SaveChangesAsync` if needed.

---

## 9. Repository Pattern

### Interface (`Domain/I<Entity>Repository.cs`)
```csharp
// Extend IRepository<TEntity, TKey> — gives GetByIdAsync, Add, Update, Remove
public interface IProductRepository : IRepository<Product, Guid>
{
    // Add module-specific query methods here
    Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default);
}
```

### Implementation (`Infrastructure/Persistence/<Entity>Repository.cs`)
```csharp
// Extend BaseRepository — provides IRepository<> implementation via EF
public sealed class ProductRepository(InventoryDbContext context)
    : BaseRepository<Product, Guid, InventoryDbContext>(context), IProductRepository
{
    public async Task<List<Product>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.ToListAsync(cancellationToken);

    public async Task<List<Product>> GetByIdsAsync(List<Guid> ids, CancellationToken cancellationToken = default) =>
        await DbSet.Where(p => ids.Contains(p.Id)).ToListAsync(cancellationToken);
}
```

**Rules:**
- Only the module's own `DbSet` — never query another module's DbContext
- No `SaveChanges` calls inside repositories — that is the UoW's job
- Complex queries can use `AsNoTracking()` in read-only paths

---

## 10. Unit of Work

### Module-specific interface (`Application/Abstractions/I<Module>UnitOfWork.cs`)
```csharp
public interface IInventoryUnitOfWork : IUnitOfWork { }
// IUnitOfWork: Task<int> SaveChangesAsync(CancellationToken)
```

### Implemented by the DbContext
```csharp
// InventoryDbContext : BaseDbContext, IInventoryUnitOfWork
// BaseDbContext : DbContext, IUnitOfWork — auto-sets CreatedAt/UpdatedAt
```

### Registration (`Infrastructure/Extensions/<Module>Module.cs`)
```csharp
services.AddScoped<IInventoryUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
```

**When to inject:**
- Commands that persist state → inject `I<Module>UnitOfWork`
- Queries → never inject UoW (read-only via repository)
- Each module injects only its own UoW

---

## 11. FluentValidation

Validators are auto-discovered and run by `ValidationBehavior<TRequest, TResponse>` (registered globally in `AddApplication`). Just create a validator class in the same assembly.

```csharp
// Application/<Feature>/<Action><Entity>/Create<Entity>CommandValidator.cs
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

**No registration needed** — `services.AddValidatorsFromAssemblies(assemblies)` in `AddApplication` picks it up automatically.

---

## 12. Mapster Mapping Config

Use Mapster when the entity → DTO mapping is non-trivial (enum → string, nested collections, property shape differences).

### Config class (`Application/<Feature>/<Module>MappingConfig.cs`)
```csharp
public static class OrderMappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<Order, OrderResponse>
            .NewConfig()
            .Map(dest => dest.Status, src => src.Status.ToString())   // enum → string
            .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemResponse>>());

        TypeAdapterConfig<OrderItem, OrderItemResponse>.NewConfig();   // names match; no custom rules
    }
}
```

### Register at startup (`Infrastructure/Extensions/<Module>Module.cs`)
```csharp
services.AddApplication(typeof(Application.AssemblyReference).Assembly);
OrderMappingConfig.Configure();   // call after AddApplication
```

### Use in QueryHandler
```csharp
return order.Adapt<OrderResponse>();
```

**When to use Mapster vs manual:**
- Manual (`new Dto(...)`) — flat DTOs, 1–4 fields, no type conversion
- Mapster — nested objects, enum conversions, collections, 5+ fields

---

## 13. Folder Conventions

```
Application/
  Products/
    CreateProduct/
      CreateProductCommand.cs           ← Command + response record (if any)
      CreateProductCommandHandler.cs    ← Handler
      CreateProductCommandValidator.cs  ← FluentValidation (optional)
    GetProducts/
      GetProductsQuery.cs               ← Query + response DTO(s)
      GetProductsQueryHandler.cs        ← Handler
    ProductMappingConfig.cs             ← Mapster config (if needed)
  Abstractions/
    I<Module>UnitOfWork.cs
```

---

## 15. Transactional Outbox Pattern (per-module)

### How it works

```
entity.RaiseDomainEvent(new XyzDomainEvent(...))   [in-memory, in Entity<TKey>]
      ↓
unitOfWork.SaveChangesAsync()
      └─ OutboxMessagesInterceptor (SaveChangesInterceptor)
           serialises domain events → OutboxMessage rows (same DB transaction, same schema)
      ↓
ProcessOutboxMessagesJob (Quartz, every 30s)
      └─ OutboxMessageProcessor<TDbContext> reads unprocessed rows
      └─ JsonDeserialise → IDomainEvent
      └─ publisher.Publish(DomainEventNotification<T>)   [MediatR]
      ↓
IDomainEventHandler<T>.Handle(DomainEventNotification<T>)
      └─ converts to integration event
      └─ IEventBus.PublishAsync(IntegrationEvent)        [MassTransit → RabbitMQ]
      ↓
Consumers in other modules react
```

### Key types (all in Common — no module work needed)

| Type | Location | Role |
|---|---|---|
| `OutboxMessage` | `Common.Infrastructure/Outbox/` | EF entity persisted to `<schema>.OutboxMessages` |
| `OutboxMessagesInterceptor` | `Common.Infrastructure/Outbox/` | Registered as singleton; writes rows atomically |
| `OutboxMessageProcessor<TDbContext>` | `Common.Infrastructure/Outbox/` | Generic; one registration per module |
| `ProcessOutboxMessagesJob` | `Common.Infrastructure/Jobs/` | Quartz job, `[DisallowConcurrentExecution]` |
| `DomainEventNotification<T>` | `Common.Application/Abstractions/` | MediatR `INotification` wrapper (avoids MediatR dep in Domain) |
| `IDomainEventHandler<T>` | `Common.Application/Abstractions/` | `INotificationHandler<DomainEventNotification<T>>` |
| `IEventBus` | `Common.Application/Abstractions/` | Abstraction over MassTransit for Application layer |
| `MassTransitEventBus` | `Common.Infrastructure/EventBus/` | `IEventBus` → `IPublishEndpoint.Publish` |

### Per-module wiring (Infrastructure/Extensions/<Module>Module.cs)

Two changes per module — **this is all that is needed per module**:

```csharp
// 1. Inject interceptor into AddDbContext
services.AddDbContext<InventoryDbContext>((sp, opts) =>
{
    opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb"));
    opts.AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>());   // ← add this
});

// 2. Register the generic processor for this module's DbContext
services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<InventoryDbContext>>();  // ← add this
```

The `OutboxMessagesInterceptor` singleton and Quartz job are already registered globally in `InfrastructureExtensions.AddInfrastructure()`.

### Per-module migration

After wiring, add a migration to create `<schema>.OutboxMessages`:

```bash
dotnet ef migrations add AddOutboxMessages \
  --project src/Modules/<Module>/Ochestrator.Modules.<Module>.Infrastructure \
  --startup-project src/Modules/<Module>/Ochestrator.Modules.<Module>.Infrastructure \
  --context <Module>DbContext
```

The `OutboxMessages` table config is inherited from `BaseDbContext.OnModelCreating`, so nothing extra is needed in the module's `DbContext`.

### Domain event handler pattern

```csharp
// Application/<Feature>/<Action>/XyzDomainEventHandler.cs
public sealed class OrderCreatedDomainEventHandler(
    IOrderRepository orderRepository,
    IEventBus eventBus) : IDomainEventHandler<OrderCreatedDomainEvent>
{
    public async Task Handle(
        DomainEventNotification<OrderCreatedDomainEvent> notification,
        CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        // Load the aggregate if the event doesn't carry all needed data
        var order = await orderRepository.GetByIdAsync(domainEvent.OrderId, cancellationToken);
        if (order is null) return;

        // Convert to integration event and publish to RabbitMQ
        await eventBus.PublishAsync(new OrderCreatedIntegrationEvent(
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            order.TotalAmount,
            domainEvent.OccurredOn), cancellationToken);
    }
}
```

**Rules:**
- One handler per domain event per concern (multiple handlers for the same event are fine — MediatR fans out)
- Do NOT inject `IPublishEndpoint` directly in handlers — use `IEventBus`
- Do NOT inject `I<Module>UnitOfWork` — the processor already committed; just read and publish
- The handler is auto-discovered by `AddApplication(assembly)` — no manual registration needed

### Domain event definition (no change to existing pattern)

```csharp
// Domain/Events/OrderCreatedDomainEvent.cs
public sealed record OrderCreatedDomainEvent(Guid OrderId, string CustomerId) : DomainEvent;

// Inside the entity constructor/factory:
RaiseDomainEvent(new OrderCreatedDomainEvent(id, customerId));
```

---

## 14. Defining DDD Types from Requirements

When given a requirement, apply this checklist:

| Question | Decision |
|---|---|
| Does it have identity and lifecycle? | → Aggregate root (`AuditableGuidEntity`) |
| Is it owned by an aggregate, no independent lifecycle? | → Child entity (`Entity<Guid>`) |
| Is it just a grouping of data with no identity? | → Value Object (C# `record`) |
| Does something meaningful happen that other parts should know about? | → Domain Event |
| Does another module need to react? | → Integration Event (in `IntegrationEvents` project) |
| Is it a business operation that changes state? | → Command |
| Is it a business operation that only reads state? | → Query |
| Should it fail gracefully? | → Return `Result.Failure<T>(Error...)` |
| Is input from outside the system? | → Validate with FluentValidation |

### Example: "Allow customers to update their shipping address"

1. `Address` — value object (`record`) with `Street`, `City`, `PostalCode`
2. `Order` entity gets `ShippingAddress` property + `UpdateShippingAddress(Address)` method
3. `UpdateShippingAddressCommand(Guid OrderId, string Street, string City, string PostalCode) : ICommand`
4. `UpdateShippingAddressCommandHandler` — loads order, calls method, saves via UoW
5. `UpdateShippingAddressCommandValidator` — validate non-empty fields
6. Optionally: `ShippingAddressUpdatedDomainEvent` raised in entity if others care intra-module
