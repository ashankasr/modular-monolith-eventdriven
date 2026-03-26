# ModularMonolithEventDriven Backend Architetcure

## Project Overview

This is a **.NET 9 Modular Monolith** that demonstrates two distributed transaction patterns side-by-side using Clean Architecture and Domain-Driven Design (DDD) principles.

### Core Principles

- **Clean Architecture**: Strict dependency inversion with domain at the center
- **Domain-Driven Design**: Business logic encapsulated in domain entities with private setters and factory methods
- **CQRS**: Command-Query Responsibility Segregation using MediatR
- **Modular Design**: Business domains separated into independent modules with schema isolation
- **Event-Driven Architecture**: Integration events over RabbitMQ (MassTransit) for loose coupling between modules
- **Single Database, Schema-per-Module**: One SQL Server database with separate schemas (`orders`, `inventory`, `payments`, `notifications`)

### Key Technologies

| Concern | Library | Version |
|---|---|---|
| Message broker | MassTransit + RabbitMQ | 8.3.6 |
| Saga persistence | MassTransit.EntityFrameworkCore | 8.3.6 |
| ORM | Entity Framework Core (SQL Server) | 9.0.3 |
| CQRS | MediatR | 12.4.1 |
| Validation | FluentValidation | 11.11.0 |
| Mapping | Mapster | 7.4.0 |
| Logging | Serilog.AspNetCore | 8.0.3 |
| API docs | Scalar.AspNetCore | 2.1.7 |

NuGet versions are centrally managed in `Directory.Packages.props`.

---

## High-Level Folder Structure

```
src/
├── Api/
│   └── ModularMonolithEventDriven.Api/              # Entry point — wires all modules
├── Common/
│   ├── ModularMonolithEventDriven.Common.Domain/    # Base entities, Result<T>, Error, IDomainEvent
│   ├── ModularMonolithEventDriven.Common.Application/  # ICommand/IQuery, IRepository, IUnitOfWork, pipeline behaviors
│   └── ModularMonolithEventDriven.Common.Infrastructure/ # BaseDbContext, BaseRepository
└── Modules/
    ├── Orders/
    ├── Inventory/
    ├── Payments/
    └── Notifications/
```

### Module Structure Pattern

Each business module follows this consistent 5-project structure:

```
ModularMonolithEventDriven.Modules.{ModuleName}.Domain/
ModularMonolithEventDriven.Modules.{ModuleName}.Application/
ModularMonolithEventDriven.Modules.{ModuleName}.Infrastructure/
ModularMonolithEventDriven.Modules.{ModuleName}.IntegrationEvents/
ModularMonolithEventDriven.Modules.{ModuleName}.Presentation/
```

**Layer responsibilities:**

| Layer | Responsibility |
|---|---|
| Domain | Entities, value objects, domain events, repository interfaces, domain errors |
| Application | Commands, queries, MediatR handlers, `AssemblyReference`, module-specific `IUnitOfWork` |
| Infrastructure | DbContext, EF migrations, repository implementations, MassTransit consumers, module DI extension |
| IntegrationEvents | Message contracts published to / consumed from RabbitMQ (records) |
| Presentation | Minimal API endpoint mappings (static extension methods on `IEndpointRouteBuilder`) |

**Modules:**

| Module | DB Schema | IUnitOfWork |
|---|---|---|
| Orders | `orders` | `IOrdersUnitOfWork` |
| Inventory | `inventory` | `IInventoryUnitOfWork` |
| Payments | `payments` | `IPaymentsUnitOfWork` |
| Notifications | `notifications` | *(none — read-only)* |

---

## Naming Conventions

- **Namespace prefix**: `ModularMonolithEventDriven`
- **Module projects**: `ModularMonolithEventDriven.Modules.<Module>.<Layer>`
- **Common projects**: `ModularMonolithEventDriven.Common.<Layer>`
- Use PascalCase for classes, methods, properties, namespaces
- Use camelCase for private fields and parameters
- Prefix interfaces with `I` (e.g., `IOrderRepository`)
- One class per file; file name matches class name

---

## Common Layer

### Common.Domain

#### Base Entity Classes

**`Entity<TKey>`** — base for all entities with domain event support

```csharp
public abstract class Entity<TKey> : IEquatable<Entity<TKey>>
    where TKey : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public TKey Id { get; protected set; } = default!;
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
```

**`GuidEntity`** — entity with `Guid` primary key

```csharp
public abstract class GuidEntity : Entity<Guid>
{
    protected GuidEntity(Guid id) : base(id) { }
    protected GuidEntity() { }
}
```

**`AuditableEntity<TKey>`** — adds `CreatedAt` and `UpdatedAt`

```csharp
public abstract class AuditableEntity<TKey> : Entity<TKey>
    where TKey : notnull
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**`AuditableGuidEntity`** — most common base for business entities

```csharp
public abstract class AuditableGuidEntity : AuditableEntity<Guid> { }
```

#### When to use which base class

| Scenario | Base class |
|---|---|
| Business entity needing audit fields + Guid ID | `AuditableGuidEntity` ✅ most common |
| Child/dependent entity (e.g. `OrderItem`) | `Entity<Guid>` or `GuidEntity` |
| Simple entity, no audit needed | `GuidEntity` |

#### Result Pattern

**`Error`** — immutable record describing a failure

```csharp
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided.");

    public static Error NotFound(string code, string description) => new(code, description);
    public static Error Validation(string code, string description) => new(code, description);
    public static Error Conflict(string code, string description) => new(code, description);
    public static Error Failure(string code, string description) => new(code, description);
}
```

**`Result` / `Result<TValue>`** — wraps success or failure

```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("...");
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
```

#### Domain Errors Pattern

Static error class per domain aggregate:

```csharp
// Modules/Orders/Domain/Errors/OrderErrors.cs
public static class OrderErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Order.NotFound", $"Order with id '{id}' was not found.");

    public static readonly Error EmptyItems =
        Error.Validation("Order.EmptyItems", "Order must have at least one item.");
}
```

- Static class per domain entity
- Factory methods for parameterized errors (e.g. `NotFound(Guid id)`)
- Static readonly fields for fixed errors
- Use `Error.NotFound()`, `Error.Validation()`, `Error.Conflict()`, `Error.Failure()` factories

#### Domain Events

```csharp
public interface IDomainEvent : INotification { }

public sealed record OrderCreatedDomainEvent(Guid OrderId, string CustomerId) : IDomainEvent;
```

- Records implementing `IDomainEvent`
- Raised inside entity methods via `RaiseDomainEvent()`
- Handled by MediatR `INotificationHandler<T>` in Application layer if needed

### Common.Application

#### CQRS Interfaces

```csharp
// Commands (state-changing, returns Result or Result<T>)
public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand { }
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }

// Queries (data retrieval, always returns Result<T>)
public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
```

#### Repository Interface

```csharp
public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    void Add(TEntity entity);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
```

#### IUnitOfWork

```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Module-specific UoW interfaces extend this:

```csharp
// Application layer of each module
public interface IOrdersUnitOfWork : IUnitOfWork { }
public interface IInventoryUnitOfWork : IUnitOfWork { }
public interface IPaymentsUnitOfWork : IUnitOfWork { }
```

Each module's `DbContext` implements both `BaseDbContext` (which implements `IUnitOfWork`) and the module-specific interface. This avoids DI naming conflicts when all four DbContexts are registered.

#### Pipeline Behaviors

Registered globally via `AddApplication()`:

- **`LoggingBehavior<TRequest, TResponse>`** — logs request name before/after handling
- **`ValidationBehavior<TRequest, TResponse>`** — runs FluentValidation validators; throws `ValidationException` on failure

`AddApplication()` extension wires MediatR + behaviors + FluentValidation for a given assembly:

```csharp
services.AddApplication(typeof(Application.AssemblyReference).Assembly);
```

### Common.Infrastructure

#### BaseDbContext

```csharp
public abstract class BaseDbContext(DbContextOptions options) : DbContext(options), IUnitOfWork
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically sets CreatedAt on Add, UpdatedAt on Add/Modify
        SetAuditableProperties();
        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

**You do not need to manually set `CreatedAt` or `UpdatedAt`** — `BaseDbContext.SaveChangesAsync` handles this for all `AuditableEntity<Guid>` entries.

#### BaseRepository

```csharp
public abstract class BaseRepository<TEntity, TKey, TDbContext>(TDbContext context)
    : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
    where TDbContext : DbContext
{
    protected readonly TDbContext Context = context;
    protected DbSet<TEntity> DbSet => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    public void Add(TEntity entity) => DbSet.Add(entity);
    public void Update(TEntity entity) => DbSet.Update(entity);
    public void Remove(TEntity entity) => DbSet.Remove(entity);
}
```

---

## Domain Entity Pattern

Entities use **private constructors**, **factory methods**, and **behavioral methods** to enforce domain invariants.

```csharp
public sealed class Order : AuditableGuidEntity
{
    private readonly List<OrderItem> _items = [];

    private Order() { }   // Required for EF Core

    private Order(Guid id, string customerId, string customerEmail, List<OrderItem> items) : base(id)
    {
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        _items = items;
        Status = OrderStatus.Pending;
        TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity);

        RaiseDomainEvent(new OrderCreatedDomainEvent(id, customerId));
    }

    public string CustomerId { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Factory method — the only public way to create an Order
    public static Order Create(Guid id, string customerId, string customerEmail, List<OrderItem> items)
    {
        ArgumentException.ThrowIfNullOrEmpty(customerId);
        ArgumentException.ThrowIfNullOrEmpty(customerEmail);
        if (items.Count == 0)
            throw new ArgumentException("Order must have at least one item.", nameof(items));

        return new Order(id, customerId, customerEmail, items);
    }

    // Behavioral methods instead of public setters
    public void MarkAsStockReserved() => Status = OrderStatus.StockReserved;
    public void MarkAsPaymentProcessed() => Status = OrderStatus.PaymentProcessed;
    public void MarkAsCompleted() => Status = OrderStatus.Completed;
    public void MarkAsFailed(string reason) { Status = OrderStatus.Failed; FailureReason = reason; }
}
```

**Rules:**
- Seal concrete entity classes
- Private constructor (EF Core requires a parameterless one)
- All public properties have `private set`
- Aggregate root exposes child collections as `IReadOnlyList<T>`
- Raise domain events inside the constructor or behavioral methods
- Do **not** receive infrastructure concerns (UoW, repositories) inside domain entities

---

## Repository Pattern

### Domain Layer — interface

```csharp
// Modules/Orders/Domain/IOrderRepository.cs
public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}
```

### Infrastructure Layer — implementation

```csharp
// Modules/Orders/Infrastructure/Persistence/OrderRepository.cs
public sealed class OrderRepository(OrdersDbContext context)
    : BaseRepository<Order, Guid, OrdersDbContext>(context), IOrderRepository
{
    public override async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await Context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Context.Orders.Include(o => o.Items).ToListAsync(cancellationToken);
}
```

---

## CQRS Pattern

### Command Handler

```csharp
public sealed record PlaceOrderCommand(
    string CustomerId,
    string CustomerEmail,
    List<PlaceOrderItemDto> Items) : ICommand<PlaceOrderResponse>;

public sealed record PlaceOrderResponse(Guid OrderId);

public sealed class PlaceOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrdersUnitOfWork unitOfWork) : ICommandHandler<PlaceOrderCommand, PlaceOrderResponse>
{
    public async Task<Result<PlaceOrderResponse>> Handle(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var items = command.Items
            .Select(i => new OrderItem(Guid.NewGuid(), Guid.Empty, i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = Order.Create(Guid.NewGuid(), command.CustomerId, command.CustomerEmail, items);

        orderRepository.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResponse(order.Id);
    }
}
```

### Query Handler

```csharp
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

public sealed class GetOrderQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(query.OrderId, cancellationToken);

        if (order is null)
            return Result.Failure<OrderResponse>(OrderErrors.NotFound(query.OrderId));

        return order.Adapt<OrderResponse>();
    }
}
```

### Validation

Add a FluentValidation validator alongside the command — `ValidationBehavior` picks it up automatically:

```csharp
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.CustomerEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

---

## DbContext Pattern

Each module's DbContext inherits `BaseDbContext` and implements its module-specific `IUnitOfWork`:

```csharp
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options)
    : BaseDbContext(options), IOrdersUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("orders");  // ← schema isolation

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerId).IsRequired().HasMaxLength(100);
            b.Property(o => o.Status).HasConversion<string>().IsRequired();
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.HasMany(o => o.Items)
             .WithOne()
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

**Rules:**
- Always call `base.OnModelCreating(modelBuilder)` first
- Set `modelBuilder.HasDefaultSchema("schema_name")` — one schema per module
- Configure entities inline with the Fluent API in `OnModelCreating`
- Expose `DbSet<T>` properties for aggregate roots only
- Register the IUnitOfWork mapping in the module extension: `services.AddScoped<IOrdersUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>())`

---

## Integration Events Pattern

Integration events are **immutable records** in the `IntegrationEvents` project. They are the only shared contracts between modules over RabbitMQ.

```csharp
// Orders.IntegrationEvents
public sealed record OrderPlacedEvent(
    Guid OrderId,
    string CustomerId,
    string CustomerEmail,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    DateTime PlacedAt);

// Inventory.IntegrationEvents — success event
public sealed record StockReservedEvent(
    Guid CorrelationId,
    Guid OrderId,
    Guid ReservationId,
    DateTime ReservedAt);

// Inventory.IntegrationEvents — failure event
public sealed record StockReservationFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason,
    DateTime FailedAt);
```

**Rules:**
- Use `sealed record` — never classes
- Include `CorrelationId` (Guid) on all saga-related events for correlation
- Include `OrderId` for cross-module tracing
- Include a timestamp (e.g. `PlacedAt`, `ReservedAt`, `FailedAt`)
- Include a `Reason` string on failure events
- No business logic inside integration events — data contracts only

---

## MassTransit Consumer Pattern

Consumers live in the module's `Infrastructure` project:

```csharp
public sealed class ReserveStockCommandConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<ReserveStockCommandConsumer> logger) : IConsumer<ReserveStockCommand>
{
    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var msg = context.Message;

        // --- business logic ---

        // On success:
        await context.Publish(new StockReservedEvent(msg.CorrelationId, msg.OrderId, reservationId, DateTime.UtcNow));

        // On failure:
        await context.Publish(new StockReservationFailedEvent(msg.CorrelationId, msg.OrderId, reason, DateTime.UtcNow));
    }
}
```

**Rules:**
- Implement `IConsumer<T>` directly — no base class needed
- Constructor-inject repositories, DbContext, and `ILogger<T>`
- Publish result events via `context.Publish()`
- Log with `[CHOREOGRAPHY]` or `[ORCHESTRATION]` prefix for observability
- Register all consumers in `Program.cs` inside `AddMassTransit()`

---

## Saga (Orchestration) Pattern

The `OrderSaga` state machine lives in `Orders.Application/Saga/`:

```csharp
public sealed class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    public State OrderSubmitted { get; private set; } = null!;
    public State StockReserved { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<StartOrderSagaMessage> OrderSagaStarted { get; private set; } = null!;
    public Event<StockReservedEvent> StockWasReserved { get; private set; } = null!;
    // ...

    public OrderSaga(ILogger<OrderSaga> logger)
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderSagaStarted, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockWasReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        Initially(
            When(OrderSagaStarted)
                .Then(ctx => { /* store saga data */ })
                .PublishAsync(ctx => ctx.Init<ReserveStockCommand>(new ReserveStockCommand(...)))
                .TransitionTo(OrderSubmitted));

        During(OrderSubmitted,
            When(StockWasReserved)
                .PublishAsync(ctx => ctx.Init<ProcessPaymentCommand>(new ProcessPaymentCommand(...)))
                .TransitionTo(StockReserved),

            When(StockReservationFailed)
                .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
                .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new SendOrderNotificationCommand(...)))
                .TransitionTo(Failed)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}
```

**`OrderSagaState`** — persisted in `orders.OrderSagaState`:

```csharp
public sealed class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? PaymentId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

**Saga Registration in Program.cs:**

```csharp
x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
    .EntityFrameworkRepository(r =>
    {
        r.ConcurrencyMode = ConcurrencyMode.Optimistic;
        r.ExistingDbContext<OrdersDbContext>();
    });
```

**Saga Flow:**

```
POST /api/orders/orchestration
  → StartOrderSagaCommand → [Saga] ReserveStockCommand → Inventory
      ↳ StockReservedEvent → [Saga] ProcessPaymentCommand → Payments
          ↳ PaymentProcessedEvent → [Saga] SendOrderNotificationCommand → Notifications → ✅ Completed
          ↳ PaymentFailedEvent → [Saga] ReleaseStockCommand (compensation) + Notification → ❌ Failed
      ↳ StockReservationFailedEvent → [Saga] SendOrderNotificationCommand → ❌ Failed
```

---

## Choreography Flow

```
POST /api/orders/choreography
  → PlaceOrderCommand → Order created → publishes OrderPlacedEvent
      → Inventory consumer: reserves stock → publishes StockReservedEvent
          → Payments consumer: processes payment → publishes PaymentProcessedEvent
              → Notifications consumer: sends notification
```

Consumers react autonomously — no central coordinator. Each consumer publishes the next event in the chain.

---

## Endpoint Pattern

Endpoints use static extension methods on `IEndpointRouteBuilder` — **no `IEndpoint` interface, no controllers**:

```csharp
// Presentation/OrdersEndpoints.cs
public static class OrdersEndpoints
{
    public static IEndpointRouteBuilder MapOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapPost("/choreography", async (PlaceOrderRequest request, ISender sender) =>
        {
            var command = new PlaceOrderCommand(request.CustomerId, request.CustomerEmail, request.Items);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Ok(new { result.Value.OrderId })
                : Results.BadRequest(result.Error);
        })
        .WithSummary("Place order (Choreography pattern)");

        group.MapGet("/{orderId:guid}", async (Guid orderId, ISender sender) =>
        {
            var result = await sender.Send(new GetOrderQuery(orderId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        })
        .WithSummary("Get order by ID");

        return app;
    }
}
```

**Rules:**
- Static class, static extension method returning `IEndpointRouteBuilder`
- Use `MapGroup("/api/{module}")` with `WithTags("{Module}")` for Scalar grouping
- Inject `ISender` (MediatR) for CQRS dispatch
- Use `Results.Ok`, `Results.NotFound`, `Results.BadRequest` — no custom wrappers
- Request DTOs as `sealed record` types defined in the same file
- Register in `Program.cs`: `app.Map{Module}Endpoints()`

---

## Module Registration Pattern

Each module exposes a single extension method in `Infrastructure/Extensions/{Module}ModuleExtensions.cs`:

```csharp
public static class InventoryModuleExtensions
{
    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb")));

        services.AddScoped<IInventoryUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();

        services.AddApplication(typeof(Application.AssemblyReference).Assembly);

        return services;
    }
}
```

**Rules:**
- One `Add{Module}Module()` extension per module
- Register DbContext with the shared connection string key `"ModularMonolithEventDrivenDb"`
- Map module-specific IUnitOfWork to the DbContext instance
- Register all repositories as `Scoped`
- Call `AddApplication(assembly)` to register MediatR handlers + validators
- Called in `Program.cs` before `app.Build()`

---

## AssemblyReference Pattern

Each Application layer project has an empty marker class for assembly scanning:

```csharp
// Modules/Orders/Application/AssemblyReference.cs
namespace ModularMonolithEventDriven.Modules.Orders.Application;

public static class AssemblyReference { }
```

Usage:

```csharp
services.AddApplication(typeof(Application.AssemblyReference).Assembly);
```

Add this class to every new Application project you create.

---

## Program.cs (API Host) Wiring

Full wiring order in `src/Api/ModularMonolithEventDriven.Api/Program.cs`:

1. Serilog configured from `appsettings.json`
2. All 4 module `Add{Module}Module()` extensions called
3. MassTransit configured with:
   - All choreography consumers registered
   - All orchestration consumers registered
   - Saga state machine registered with EF Core repository (optimistic concurrency)
   - RabbitMQ transport with retry policy: 3 attempts at 100ms → 500ms → 1s
4. OpenAPI + Scalar UI (development only, theme Purple)
5. `app.Map{Module}Endpoints()` for all 4 modules
6. Auto-migrations applied on startup via `Database.MigrateAsync()` for all 4 DbContexts

---

## EF Migrations

Migrations live in each module's `Infrastructure` project. The API project serves as the startup project for migrations:

```bash
# Add migration
dotnet ef migrations add <Name> \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
  --context OrdersDbContext

# Apply manually (also runs automatically on startup)
dotnet ef database update \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
  --context OrdersDbContext
```

Available contexts: `OrdersDbContext`, `InventoryDbContext`, `PaymentsDbContext`, `NotificationsDbContext`

---

## Logging Conventions

- Inject `ILogger<T>` via constructor
- Prefix log messages with the pattern context:
  - `[CHOREOGRAPHY]` for choreography-path consumers
  - `[ORCHESTRATION]` or `[SAGA]` for saga-path consumers and the state machine
- Use structured logging message templates — never string interpolation:

```csharp
logger.LogInformation("[SAGA] Order {OrderId} saga started", sagaState.OrderId);
logger.LogWarning("[ORCHESTRATION] Stock reservation FAILED for Order {OrderId}: {Reason}", orderId, reason);
```

---

## Error Handling

- Use `Result<T>` for all expected business failures — never throw for expected errors
- Throw exceptions only for programming errors (null arguments, invalid state)
- Return `Results.BadRequest(result.Error)` or `Results.NotFound(result.Error)` from endpoints
- `ValidationBehavior` converts FluentValidation failures to `ValidationException`

---

## Adding a New Module

When adding a new module, create all 5 projects following the naming convention, then:

1. **Domain** — entities (inherit `AuditableGuidEntity`), repository interfaces, domain errors (`{Entity}Errors`), domain events
2. **Application** — commands/queries/handlers, validators, `AssemblyReference`, `I{Module}UnitOfWork`
3. **Infrastructure** — DbContext (schema + entity config), repositories, consumers, `{Module}ModuleExtensions`
4. **IntegrationEvents** — `sealed record` event/command contracts
5. **Presentation** — `{Module}Endpoints.cs` static extension class
6. **Program.cs** — add `Add{Module}Module()`, `Map{Module}Endpoints()`, register consumers in MassTransit block
