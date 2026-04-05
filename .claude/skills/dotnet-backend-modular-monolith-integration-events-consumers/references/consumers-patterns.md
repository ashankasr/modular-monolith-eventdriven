# Integration Events & Consumers — Complete Reference

---

## 1. Integration Event Contracts

### Where they live

Each module owns one `IntegrationEvents` project:

```
Modules/<Name>/ModularMonolithEventDriven.Modules.<Name>.IntegrationEvents/
  <EventName>.cs        ← published by this module
  <CommandName>.cs      ← saga commands sent TO this module (also live here)
  AssemblyReference.cs
```

The `IntegrationEvents` project has **no project references** — it is a pure contract library.

### Shape

```csharp
// Events — something happened (past tense, immutable)
public sealed record OrderCancelledEvent(
    Guid OrderId,
    string Reason,
    DateTime CancelledAt);

// Saga commands — instruction from the saga to a module
public sealed record ReserveStockCommand(
    Guid CorrelationId,    // required for saga correlation
    Guid OrderId,
    List<OrderItemDto> Items);

// Result events — published back to the saga after executing a command
public sealed record StockReservedEvent(
    Guid CorrelationId,    // must match the incoming CorrelationId
    Guid OrderId,
    Guid ReservationId,
    DateTime OccurredAt);

public sealed record StockReservationFailedEvent(
    Guid CorrelationId,
    Guid OrderId,
    string Reason,
    DateTime OccurredAt);
```

**Rules:**
- Always `sealed record` — immutable value type
- Include `CorrelationId` on any event/command that participates in a saga
- Include a timestamp (`OccurredAt` / `CancelledAt`) for observability
- No domain objects, no EF entities — only primitive types and nested records

---

## 2. Cross-Module Project References

A consumer in module A that reacts to an event published by module B needs to reference B's `IntegrationEvents` project:

```xml
<!-- Inventory.Infrastructure.csproj — needs Orders' event contracts -->
<ProjectReference Include="..\..\..\Modules\Orders\
    ModularMonolithEventDriven.Modules.Orders.IntegrationEvents\
    ModularMonolithEventDriven.Modules.Orders.IntegrationEvents.csproj" />
```

**Allowed references:**
- `Infrastructure` → another module's `IntegrationEvents` ✅
- `Application` → another module's `IntegrationEvents` ✅ (for saga commands)
- `Infrastructure` → another module's `Domain` or `Application` ❌ — never

---

## 3. Consumer Anatomy

```
Infrastructure/
  Consumers/
    <EventName>Consumer.cs     ← one consumer per message type (preferred)
```

### Full consumer shape

```csharp
// CHOREOGRAPHY example: reacts to OrderCancelledEvent, no reply needed
public sealed class OrderCancelledInventoryConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<OrderCancelledInventoryConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Inventory received OrderCancelledEvent for Order {OrderId}", msg.OrderId);

        var reservation = await reservationRepository.GetByOrderIdAsync(msg.OrderId, context.CancellationToken);
        if (reservation is null)
        {
            logger.LogInformation("[CHOREOGRAPHY] No reservation found for Order {OrderId} — nothing to release", msg.OrderId);
            return;  // idempotent — safe to do nothing
        }

        var productIds = reservation.Items.Select(i => i.ProductId).ToList();
        var products = await productRepository.GetByIdsAsync(productIds, context.CancellationToken);

        foreach (var item in reservation.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            product?.ReleaseStock(item.Quantity);
        }

        reservation.Release();
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] Stock released for cancelled Order {OrderId}", msg.OrderId);
        // No reply — choreography consumers are fire-and-forget
    }
}
```

**Consumer dependency rules:**
- Inject own module's repositories only — never another module's repository
- Inject the module's `DbContext` directly (not `IUnitOfWork`) — consumers live in Infrastructure, DbContext is available
- Inject `ILogger<T>` — always log what was received with pattern `[ORCHESTRATION]` or `[CHOREOGRAPHY]`
- Do NOT inject `ISender` (MediatR) from consumers — consumers ARE the handler; no need to delegate further
- `context.CancellationToken` — pass through to all async calls

---

## 4. Orchestration Consumer (Saga Command → Result Event)

The saga sends a command; the consumer does the work and publishes a success or failure event back.

```csharp
// ORCHESTRATION: Responds to ReserveStockCommand from the Saga
public sealed class ReserveStockCommandConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<ReserveStockCommandConsumer> logger) : IConsumer<ReserveStockCommand>
{
    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var msg = context.Message;
        logger.LogInformation("[ORCHESTRATION] Inventory received ReserveStockCommand for Order {OrderId}", msg.OrderId);

        // --- failure path: publish failure event, return early ---
        var products = await productRepository.GetByIdsAsync(
            msg.Items.Select(i => i.ProductId).ToList(), context.CancellationToken);

        foreach (var item in msg.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null || !product.HasSufficientStock(item.Quantity))
            {
                var reason = product is null
                    ? $"Product {item.ProductId} not found"
                    : $"Insufficient stock for {product.Name}";

                await context.Publish(new StockReservationFailedEvent(
                    msg.CorrelationId, msg.OrderId, reason, DateTime.UtcNow));
                return;   // ← early return on failure, before any state change
            }
        }

        // --- success path: mutate state, save, publish success event ---
        var reservation = new StockReservation(Guid.NewGuid(), msg.OrderId,
            msg.Items.Select(i => new ReservationItem(i.ProductId, i.Quantity)).ToList());

        foreach (var item in msg.Items)
            products.First(p => p.Id == item.ProductId).ReserveStock(item.Quantity);

        reservationRepository.Add(reservation);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        await context.Publish(new StockReservedEvent(
            msg.CorrelationId, msg.OrderId, reservation.Id, DateTime.UtcNow));
    }
}
```

**Pattern:**
1. Validate / check preconditions
2. On failure → `context.Publish(FailureEvent(...))` → `return`
3. On success → mutate domain, save, `context.Publish(SuccessEvent(...))`
4. Always carry `CorrelationId` through from `msg.CorrelationId` → the result event

---

## 5. Choreography Consumer (Autonomous Reaction, No Reply)

Published to the bus; every subscriber reacts independently. No correlation ID needed.

```csharp
// CHOREOGRAPHY: Reacts to OrderCancelledEvent — no saga, no reply
public sealed class OrderCancelledPaymentConsumer(
    IPaymentRepository paymentRepository,
    PaymentsDbContext dbContext,
    ILogger<OrderCancelledPaymentConsumer> logger) : IConsumer<OrderCancelledEvent>
{
    public async Task Consume(ConsumeContext<OrderCancelledEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("[CHOREOGRAPHY] Payments received OrderCancelledEvent for Order {OrderId}", msg.OrderId);

        var payment = await paymentRepository.GetByOrderIdAsync(msg.OrderId, context.CancellationToken);
        if (payment is null) return;   // idempotent guard

        payment.Refund();
        await dbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("[CHOREOGRAPHY] Payment refunded for cancelled Order {OrderId}", msg.OrderId);
        // No reply published — other modules don't need to know
    }
}
```

---

## 6. Compensation Consumer (Undo a Previous Step)

Triggered by the saga when a downstream step fails. Reverses a previously committed action.

```csharp
// ORCHESTRATION: ReleaseStockCommand — compensation for failed payment
public sealed class ReleaseStockCommandConsumer(
    IProductRepository productRepository,
    IStockReservationRepository reservationRepository,
    InventoryDbContext dbContext,
    ILogger<ReleaseStockCommandConsumer> logger) : IConsumer<ReleaseStockCommand>
{
    public async Task Consume(ConsumeContext<ReleaseStockCommand> context)
    {
        var msg = context.Message;

        var reservation = await reservationRepository.GetByIdAsync(msg.ReservationId, context.CancellationToken);
        if (reservation is null) return;   // already released or never created — idempotent

        var products = await productRepository.GetByIdsAsync(
            reservation.Items.Select(i => i.ProductId).ToList(), context.CancellationToken);

        foreach (var item in reservation.Items)
            products.FirstOrDefault(p => p.Id == item.ProductId)?.ReleaseStock(item.Quantity);

        reservation.Release();
        await dbContext.SaveChangesAsync(context.CancellationToken);

        // Notify the saga the compensation completed
        await context.Publish(new StockReleasedEvent(
            msg.CorrelationId, msg.OrderId, msg.ReservationId, DateTime.UtcNow));
    }
}
```

**Key rules for compensation consumers:**
- Always idempotent — check if work was already undone, return safely if so
- Publish a completion event back so the saga can continue its failure path
- The `ReservationId` / resource ID must be carried on the compensation command

---

## 7. Consumer Registration

### In the module's `<Module>Module.cs`

```csharp
public static void ConfigureConsumers(IRegistrationConfigurator configurator)
{
    // Orchestration consumers
    configurator.AddConsumer<ReserveStockCommandConsumer>();
    configurator.AddConsumer<ReleaseStockCommandConsumer>();

    // Choreography consumers
    configurator.AddConsumer<OrderCancelledInventoryConsumer>();
}
```

### In `Program.cs` (API host) — pass `ConfigureConsumers` to `AddInfrastructure`

```csharp
builder.Services.AddInfrastructure(
    [
        InventoryModule.ConfigureConsumers,   // ← add new module here
        PaymentsModule.ConfigureConsumers,
        NotificationsModule.ConfigureConsumers,
        OrdersModule.ConfigureConsumers,
    ],
    builder.Configuration);
```

`ConfigureEndpoints(context)` in `AddInfrastructure` auto-binds every registered consumer to its queue — no manual queue config needed.

---

## 8. Checklist — Adding a New Consumer

- [ ] Add integration event record to the owning module's `IntegrationEvents` project
- [ ] Add `ProjectReference` to the consumer module's `Infrastructure.csproj` for the event contract
- [ ] Create `Infrastructure/Consumers/<EventName>Consumer.cs`
- [ ] Implement `IConsumer<TMessage>` — inject only own module's repos + DbContext + ILogger
- [ ] Add idempotency guard (check if work already done, return early)
- [ ] Validate / check preconditions before state changes
- [ ] Call `dbContext.SaveChangesAsync(context.CancellationToken)` after mutations
- [ ] Publish result/reply event if orchestration; nothing if choreography
- [ ] Register in `<Module>Module.ConfigureConsumers()`

---

## 9. Decision Guide — Consumer vs Domain Event Handler

| Scenario | Use |
|---|---|
| Another module publishes an event on RabbitMQ | `IConsumer<T>` in `Infrastructure/Consumers/` |
| Same module, same transaction, domain reaction | `IDomainEventHandler<T>` via Outbox |
| Saga sends a command to this module | `IConsumer<T>` — publish success/failure event back |
| Cross-module compensation (undo a step) | `IConsumer<T>` for the compensation command |
| Need to fan out to multiple handlers for the same event | Both patterns support multiple handlers independently |
