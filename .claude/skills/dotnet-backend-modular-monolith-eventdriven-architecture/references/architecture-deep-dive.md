# Architecture Deep Dive

---

## Layer Dependency Rules (strictly enforced)

```
Presentation  →  Application  →  Domain
Infrastructure  →  Application + Common.Infrastructure
IntegrationEvents  →  (no internal project dependencies)
API Host  →  all Presentation + Common.Infrastructure
```

**Cross-module rules:**
- Infrastructure of module A **may** reference module B's `IntegrationEvents` (to consume their events)
- Infrastructure of module A must **never** reference module B's `Domain` or `Application`
- No module may reference another module's `Infrastructure` or `Presentation`

---

## Common Shared Libraries

```
Common.Domain
  └── Primitives/
      ├── Entity<T>              → base entity with DomainEvents list
      ├── AuditableEntity<T>     → adds CreatedAt, UpdatedAt
      ├── AuditableGuidEntity    → Guid-keyed auditable entity (used by most modules)
      └── Results/
          ├── Result<T>          → discriminated union: Success(value) | Failure(error)
          └── Error              → (code, message) struct

Common.Application
  └── Abstractions/
      ├── ICommand / ICommand<T>
      ├── ICommandHandler<TCmd, TResult>
      ├── IQuery<T>
      ├── IQueryHandler<TQuery, TResult>
      ├── IRepository<T>         → Add(), GetByIdAsync(), etc.
      └── IUnitOfWork            → SaveChangesAsync()
  └── Behaviors/
      ├── LoggingBehavior        → logs every request/response via MediatR pipeline
      └── ValidationBehavior     → runs FluentValidation validators before handlers
  └── Extensions/
      └── AddApplication(Assembly[])  → registers MediatR + validators from given assemblies

Common.Infrastructure
  └── Persistence/
      ├── BaseDbContext          → inherits DbContext, implements IUnitOfWork, sets audit fields
      └── BaseRepository<T,TCtx> → generic EF Core repository implementation
```

---

## Module DI Registration Pattern

Each module's `Infrastructure` project exposes one extension method:

```csharp
// Example: Orders module
public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Register DbContext
        services.AddDbContext<OrdersDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb")));

        // 2. Register module-specific IUnitOfWork (prevents DI conflicts with other modules)
        services.AddScoped<IOrdersUnitOfWork>(sp =>
            sp.GetRequiredService<OrdersDbContext>());

        // 3. Register repositories
        services.AddScoped<IOrderRepository, OrderRepository>();

        // 4. Register MediatR handlers from Application assembly
        services.AddApplication(typeof(Application.AssemblyReference).Assembly);

        return services;
    }
}
```

Called in `Program.cs`:
```csharp
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);
```

---

## Persistence: Schema-per-Module

All modules share one SQL Server database (`ModularMonolithEventDrivenDb`) but each owns a separate schema:

| Module | Schema | Tables |
|---|---|---|
| Orders | `orders` | Orders, OrderItems, OrderSagaState |
| Inventory | `inventory` | Products, StockReservations, StockReservationItems |
| Payments | `payments` | Payments |
| Notifications | `notifications` | NotificationLogs |

Set in `OnModelCreating`:
```csharp
modelBuilder.HasDefaultSchema("orders");
```

---

## CQRS via MediatR

**Commands** (state-changing):
```csharp
public sealed record PlaceOrderCommand(...) : ICommand<PlaceOrderResponse>;

public sealed class PlaceOrderCommandHandler(...) : ICommandHandler<PlaceOrderCommand, PlaceOrderResponse>
{
    public async Task<Result<PlaceOrderResponse>> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        // 1. Create domain entity
        // 2. Publish integration event (choreography)
        // 3. Persist via UnitOfWork
        return new PlaceOrderResponse(orderId);
    }
}
```

**Queries** (read-only):
```csharp
public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderResponse>;

public sealed class GetOrderQueryHandler(...) : IQueryHandler<GetOrderQuery, OrderResponse>
{
    public async Task<Result<OrderResponse>> Handle(GetOrderQuery query, CancellationToken ct) { ... }
}
```

**Pipeline behaviors** (registered globally via `AddApplication`):
1. `LoggingBehavior` — logs request name, elapsed time, and any errors
2. `ValidationBehavior` — runs FluentValidation; returns `Result.Failure(errors)` if invalid

---

## MassTransit Consumer Pattern

```csharp
public sealed class OrderPlacedInventoryConsumer(
    IProductRepository productRepo,
    InventoryDbContext dbContext,
    ILogger<OrderPlacedInventoryConsumer> logger) : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var msg = context.Message;
        // ... business logic ...
        await context.Publish(new StockReservedEvent(...));
    }
}
```

Consumers registered in `Program.cs` inside `AddMassTransit`:
```csharp
x.AddConsumer<OrderPlacedInventoryConsumer>();
// ...
x.UsingRabbitMq((ctx, cfg) =>
{
    cfg.ConfigureEndpoints(ctx);  // auto-binds all consumers to their message types
});
```

**Retry policy:** 3 attempts at 100ms → 500ms → 1s intervals.

---

## Choreography Flow (step-by-step)

```
POST /api/orders/choreography
  └─ PlaceOrderCommand (MediatR)
      └─ Creates Order entity
      └─ Publishes OrderPlacedEvent (RabbitMQ)
          └─ OrderPlacedInventoryConsumer (Inventory module)
              └─ Reserves stock
              └─ Publishes StockReservedEvent
                  └─ StockReservedPaymentConsumer (Payments module)
                      └─ Charges payment
                      └─ Publishes PaymentProcessedEvent
                          └─ PaymentProcessedNotificationConsumer (Notifications module)
                              └─ Sends notification
                  └─ StockReservedOrderUpdater (Orders module) → updates order status
              └─ Publishes StockReservationFailedEvent (if out of stock)
          └─ PaymentProcessedOrderUpdater / PaymentFailedOrderUpdater → updates order status
```

No central coordinator. Each module reacts to what it sees on the bus.

---

## Orchestration / Saga Flow (step-by-step)

```
POST /api/orders/orchestration
  └─ StartOrderSagaCommand (MediatR)
      └─ Publishes OrderSagaStarted (Saga correlation)
          └─ [Saga] OrderSubmitted state → sends ReserveStockCommand
              └─ ReserveStockCommandConsumer (Inventory module)
                  ↳ success: publishes StockWasReserved
                  ↳ failure: publishes StockReservationFailed
              └─ [Saga] StockWasReserved → StockReserved state → sends ProcessPaymentCommand
                  └─ ProcessPaymentCommandConsumer (Payments module)
                      ↳ success: publishes PaymentWasProcessed
                      ↳ failure: publishes PaymentFailed
                  └─ [Saga] PaymentWasProcessed → sends SendOrderNotificationCommand → Completed + Finalize
                  └─ [Saga] PaymentFailed → sends ReleaseStockCommand (compensation)
                                          → sends SendOrderNotificationCommand → Failed + Finalize
              └─ [Saga] StockReservationFailed → sends SendOrderNotificationCommand → Failed + Finalize
```

**Saga state** (`OrderSagaState`) persisted in `orders.OrderSagaState` with optimistic concurrency (`RowVersion` column).

---

## Technology Stack Rationale

| Concern | Library | Why |
|---|---|---|
| Messaging | MassTransit + RabbitMQ | Abstracts the broker; built-in saga, retry, and consumer binding |
| Saga persistence | MassTransit.EntityFrameworkCore | Reuses existing EF Core DbContext; optimistic concurrency support |
| ORM | EF Core 9 + SQL Server | Schema-per-module via `HasDefaultSchema()`; auto-migrations on startup |
| CQRS | MediatR 12 | Clean separation; pipeline behaviors for cross-cutting concerns |
| Validation | FluentValidation | MediatR pipeline behavior wires it automatically per command |
| Mapping | Mapster | Faster than AutoMapper; minimal API friendly |
| Logging | Microsoft.Extensions.Logging + OpenTelemetry | Built-in .NET logging; OTel exporter wired in `AddServiceDefaults()`; log levels via `appsettings.json` |
| API Docs | Scalar | Better DX than Swagger UI; themed OpenAPI reference |

---

## Transactional Outbox Pattern

### Problem: dual-write

Calling `publishEndpoint.Publish(...)` directly in a command handler after `SaveChangesAsync()` creates a **dual-write**: if the broker publish fails, the DB row is committed but no message was sent. There is no automatic retry — the event is lost silently.

### Solution: outbox interceptor + background processor

```
entity.RaiseDomainEvent(new SomeDomainEvent(...))
      ↓
unitOfWork.SaveChangesAsync()
      └─ OutboxMessagesInterceptor (EF SaveChangesInterceptor)
           writes OutboxMessage rows in the SAME DB transaction
      ↓  (background, Quartz job every 30 s)
OutboxMessageProcessor<TDbContext>
      reads WHERE ProcessedOnUtc IS NULL
      deserialises → IDomainEvent
      publisher.Publish(DomainEventNotification<T>)  [MediatR]
      ↓
IDomainEventHandler<T>.Handle(...)
      publishes integration message to RabbitMQ
      marks message.ProcessedOnUtc = UtcNow
```

### Critical rule — catch block must NOT set ProcessedOnUtc on failure

The processor filters `m.ProcessedOnUtc == null`. Setting `ProcessedOnUtc` in the catch block permanently skips the row on all future poll cycles — failed messages are silently dropped with no retry.

```csharp
// CORRECT
catch (Exception ex)
{
    logger.LogError(ex, "Outbox: failed to process message {Id}", message.Id);
    message.Error = ex.ToString();
    // ProcessedOnUtc intentionally NOT set — next poll retries
}

// WRONG — drops the message permanently on any transient failure
catch (Exception ex)
{
    message.Error = ex.ToString();
    message.ProcessedOnUtc = DateTime.UtcNow;  // ← never do this
}
```

### Where integration messages should be published

Move RabbitMQ/MassTransit publishes **out of command handlers** and **into domain event handlers**:

| Layer | Responsibility |
|---|---|
| Command handler | Create aggregate, call `SaveChangesAsync` — nothing else |
| Domain entity | `RaiseDomainEvent(new XyzDomainEvent(...))` on meaningful state change |
| `IDomainEventHandler<T>` | Re-fetch aggregate if needed, assemble and publish integration message |

This separates persistence from reaction, and the outbox provides the at-least-once delivery guarantee. The handler is allowed to load the full aggregate from the repository because by the time it runs, the transaction is already committed.

---

## When to Choose Modular Monolith

**Choose it when:**
- 3–15 developers; modules map to team or domain boundaries
- You want clean separation without microservices operational complexity (no service mesh, no distributed tracing overhead, single deploy)
- You may want to extract a module to a microservice later — integration events are already the contract

**Avoid it when:**
- Modules need radically different scaling profiles (e.g. one module handles 100K req/s, others handle 10)
- Modules need different tech stacks or runtime environments
- Team is >50 people and coordination cost exceeds deployment coupling cost
