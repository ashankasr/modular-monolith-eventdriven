# Saga Orchestration — Complete Reference

---

## 1. What a Saga Is

A **saga** is a long-running process that coordinates multiple steps across modules. Each step is a message exchange:

```
Saga sends command → Module consumer processes → Module publishes result event → Saga transitions
```

If any step fails, the saga executes compensating transactions in reverse to restore consistency.

This codebase uses **MassTransit's `MassTransitStateMachine<TState>`** — a state machine where every state, event, and transition is explicit C# code.

---

## 2. Anatomy of the Saga Class

```csharp
public sealed class OrderSaga : MassTransitStateMachine<OrderSagaState>
{
    private readonly ILogger<OrderSaga> _logger;

    // ── States ──────────────────────────────────────────────────────────────
    // Each State represents a point where the saga is waiting for a reply.
    // MassTransit adds Initial, Final implicitly.
    public State OrderSubmitted { get; private set; } = null!;
    public State StockReserved  { get; private set; } = null!;
    public State Completed      { get; private set; } = null!;
    public State Failed         { get; private set; } = null!;

    // ── Events ──────────────────────────────────────────────────────────────
    // Each Event is a message type the saga can receive.
    public Event<OrderSagaStartMessage>       OrderSagaStarted      { get; private set; } = null!;
    public Event<StockReservedEvent>          StockWasReserved      { get; private set; } = null!;
    public Event<StockReservationFailedEvent> StockReservationFailed{ get; private set; } = null!;
    public Event<PaymentProcessedEvent>       PaymentWasProcessed   { get; private set; } = null!;
    public Event<PaymentFailedEvent>          PaymentFailed         { get; private set; } = null!;

    public OrderSaga(ILogger<OrderSaga> logger)
    {
        _logger = logger;

        // ── Wire state field ─────────────────────────────────────────────────
        InstanceState(x => x.CurrentState);

        // ── Correlate all events by CorrelationId ───────────────────────────
        Event(() => OrderSagaStarted,       x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockWasReserved,       x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => StockReservationFailed, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentWasProcessed,    x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
        Event(() => PaymentFailed,          x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // ── Transitions ──────────────────────────────────────────────────────
        Initially( ... );      // handles events in the Initial state
        During(OrderSubmitted, ... );
        During(StockReserved,  ... );

        SetCompletedWhenFinalized();   // removes the saga row from DB once finalized
    }
}
```

---

## 3. State Machine DSL — Full Reference

### `Initially` — handles the first event (from `Initial` state)

```csharp
Initially(
    When(OrderSagaStarted)
        .Then(ctx =>
        {
            // Populate saga state from the start message
            ctx.Saga.OrderId      = ctx.Message.OrderId;
            ctx.Saga.CustomerId   = ctx.Message.CustomerId;
            ctx.Saga.CustomerEmail= ctx.Message.CustomerEmail;
            ctx.Saga.TotalAmount  = ctx.Message.TotalAmount;
            ctx.Saga.StartedAt    = DateTime.UtcNow;
            _logger.LogInformation("[SAGA] Order {OrderId} saga started.", ctx.Saga.OrderId);
        })
        .PublishAsync(ctx => ctx.Init<ReserveStockCommand>(new ReserveStockCommand(
            ctx.Saga.CorrelationId,
            ctx.Saga.OrderId,
            ctx.Message.Items)))
        .TransitionTo(OrderSubmitted));
```

### `During` — handles events while in a specific state

```csharp
During(OrderSubmitted,

    // Happy path: stock reserved → send payment command
    When(StockWasReserved)
        .Then(ctx =>
        {
            ctx.Saga.ReservationId = ctx.Message.ReservationId;
            _logger.LogInformation("[SAGA] Stock reserved for Order {OrderId}.", ctx.Saga.OrderId);
        })
        .PublishAsync(ctx => ctx.Init<ProcessPaymentCommand>(new ProcessPaymentCommand(
            ctx.Saga.CorrelationId,
            ctx.Saga.OrderId,
            ctx.Saga.CustomerId,
            ctx.Saga.CustomerEmail,
            ctx.Saga.TotalAmount)))
        .TransitionTo(StockReserved),

    // Failure path: stock failed → notify + finalize
    When(StockReservationFailed)
        .Then(ctx =>
        {
            ctx.Saga.FailureReason = ctx.Message.Reason;
            _logger.LogWarning("[SAGA] Stock reservation FAILED for Order {OrderId}. {Reason}",
                ctx.Saga.OrderId, ctx.Message.Reason);
        })
        .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new SendOrderNotificationCommand(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.CustomerEmail,
            "Failed", $"Your order failed: {ctx.Saga.FailureReason}")))
        .TransitionTo(Failed)
        .Finalize());   // ← marks saga as complete; SetCompletedWhenFinalized() removes the row
```

### Compensation inside `During`

```csharp
During(StockReserved,

    // Failure + compensation: payment failed → release stock, then notify
    When(PaymentFailed)
        .Then(ctx =>
        {
            ctx.Saga.FailureReason = ctx.Message.Reason;
            _logger.LogWarning("[SAGA] Payment FAILED for Order {OrderId}. Releasing stock.", ctx.Saga.OrderId);
        })
        // 1. Compensating transaction: release the reserved stock
        .PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new ReleaseStockCommand(
            ctx.Saga.CorrelationId,
            ctx.Saga.OrderId,
            ctx.Saga.ReservationId!.Value)))
        // 2. Notify user of failure
        .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new SendOrderNotificationCommand(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.CustomerEmail,
            "Failed", $"Your order failed during payment: {ctx.Saga.FailureReason}")))
        .TransitionTo(Failed)
        .Finalize());
```

### DSL method summary

| Method | Purpose |
|---|---|
| `When(Event)` | Matches an event in the current state block |
| `.Then(ctx => ...)` | Synchronous side-effect: update saga state, log |
| `.PublishAsync(ctx => ctx.Init<T>(new T(...)))` | Publish a message to the bus (command or event) |
| `.TransitionTo(State)` | Move to a new state; persists `CurrentState` |
| `.Finalize()` | Mark saga complete; triggers `SetCompletedWhenFinalized()` |
| `InstanceState(x => x.CurrentState)` | Tells MassTransit which property stores the state name |
| `Event(() => E, x => x.CorrelateById(...))` | Defines how incoming messages find their saga instance |
| `SetCompletedWhenFinalized()` | Deletes the saga row from DB when finalized |

---

## 4. Saga State (`OrderSagaState`)

```csharp
public sealed class OrderSagaState : SagaStateMachineInstance
{
    public Guid   CorrelationId  { get; set; }   // PK — links all messages for this saga instance
    public string CurrentState   { get; set; } = string.Empty;   // persisted state name

    // Data populated during transitions:
    public Guid    OrderId       { get; set; }
    public string  CustomerId    { get; set; } = string.Empty;
    public string  CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount   { get; set; }
    public Guid?   ReservationId { get; set; }   // set when stock reserved
    public Guid?   PaymentId     { get; set; }   // set when payment processed
    public string? FailureReason { get; set; }   // set on any failure
    public DateTime  StartedAt   { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

**Rules for saga state:**
- `CorrelationId` is the primary key — it links every message back to this instance
- Add a nullable property for each resource ID produced by a step (`ReservationId?`, `PaymentId?`)
- Store the minimum data needed to send subsequent commands — load full entities from DB in consumers
- `FailureReason` captures the first failure message for notification/diagnosis

---

## 5. Adding a New Step (Worked Example)

**Scenario:** Add a "shipping" step after payment — saga sends `CreateShipmentCommand` to the new Shipping module; on success the saga completes; on failure it compensates.

### Step 1 — Add a new `State` and two `Event` properties to `OrderSaga`

```csharp
public State PaymentProcessed { get; private set; } = null!;   // new waiting state

public Event<ShipmentCreatedEvent> ShipmentCreated { get; private set; } = null!;
public Event<ShipmentFailedEvent>  ShipmentFailed  { get; private set; } = null!;
```

### Step 2 — Wire correlation in the constructor

```csharp
Event(() => ShipmentCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
Event(() => ShipmentFailed,  x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
```

### Step 3 — Change existing `PaymentWasProcessed` transition to go to new state

```csharp
During(StockReserved,
    When(PaymentWasProcessed)
        .Then(ctx =>
        {
            ctx.Saga.PaymentId   = ctx.Message.PaymentId;
            ctx.Saga.CompletedAt = DateTime.UtcNow;
        })
        .PublishAsync(ctx => ctx.Init<CreateShipmentCommand>(new CreateShipmentCommand(
            ctx.Saga.CorrelationId,
            ctx.Saga.OrderId,
            ctx.Saga.CustomerId)))
        .TransitionTo(PaymentProcessed),   // ← was Completed before; now waits for shipment
```

### Step 4 — Add the new `During(PaymentProcessed, ...)` block

```csharp
During(PaymentProcessed,
    When(ShipmentCreated)
        .Then(ctx =>
        {
            ctx.Saga.ShipmentId  = ctx.Message.ShipmentId;
            ctx.Saga.CompletedAt = DateTime.UtcNow;
        })
        .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new SendOrderNotificationCommand(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.CustomerEmail,
            "Completed", "Your order has been shipped!")))
        .TransitionTo(Completed)
        .Finalize(),

    When(ShipmentFailed)
        .Then(ctx => ctx.Saga.FailureReason = ctx.Message.Reason)
        // Compensate: refund payment
        .PublishAsync(ctx => ctx.Init<RefundPaymentCommand>(new RefundPaymentCommand(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.PaymentId!.Value)))
        // Compensate: release stock
        .PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new ReleaseStockCommand(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.ReservationId!.Value)))
        .PublishAsync(ctx => ctx.Init<SendOrderNotificationCommand>(new SendOrderNotificationCommand(
            ctx.Saga.CorrelationId, ctx.Saga.OrderId, ctx.Saga.CustomerEmail,
            "Failed", $"Your order failed during shipping: {ctx.Saga.FailureReason}")))
        .TransitionTo(Failed)
        .Finalize());
```

### Step 5 — Add `ShipmentId` to `OrderSagaState`

```csharp
public Guid? ShipmentId { get; set; }
```

### Step 6 — Add the integration event contracts

In `Shipping.IntegrationEvents`:
```csharp
public sealed record CreateShipmentCommand(Guid CorrelationId, Guid OrderId, string CustomerId);
public sealed record ShipmentCreatedEvent(Guid CorrelationId, Guid OrderId, Guid ShipmentId, DateTime OccurredAt);
public sealed record ShipmentFailedEvent(Guid CorrelationId, Guid OrderId, string Reason, DateTime OccurredAt);
```

### Step 7 — Add the consumer in `Shipping.Infrastructure/Consumers/`

See the `integration-events-consumers` skill.

### Step 8 — Add migration for `OrderSagaState` schema change

```bash
dotnet ef migrations add AddShipmentIdToOrderSagaState \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure \
  --startup-project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure \
  --context OrdersDbContext
```

---

## 6. Saga Persistence — EF Core Registration

The saga state is stored in `orders.OrderSagaState` using `MassTransit.EntityFrameworkCore`.

### In `OrdersModule.ConfigureConsumers`

```csharp
public static void ConfigureConsumers(IRegistrationConfigurator configurator)
{
    configurator.AddSagaStateMachine<OrderSaga, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;   // uses RowVersion
            r.ExistingDbContext<OrdersDbContext>();
        });
}
```

### In `OrdersDbContext.OnModelCreating`

```csharp
modelBuilder.Entity<OrderSagaState>(b =>
{
    b.ToTable("OrderSagaState");
    b.HasKey(s => s.CorrelationId);
    b.Property(s => s.CurrentState).IsRequired().HasMaxLength(64);
    // Add new columns for each new state property:
    b.Property(s => s.ShipmentId);     // nullable Guid — no extra config needed
    b.Property(s => s.FailureReason).HasMaxLength(500);
    b.Property(s => s.TotalAmount).HasPrecision(18, 2);
});
```

---

## 7. Correlation — How Events Find the Right Saga Instance

Every message that the saga handles must carry a `CorrelationId` (a `Guid`):

```csharp
// Producer (command handler starting the saga):
var correlationId = Guid.NewGuid();
await publishEndpoint.Publish(new OrderSagaStartMessage(correlationId, orderId, ...));

// The saga instance uses this as its primary key:
public Guid CorrelationId { get; set; }   // in OrderSagaState

// The saga correlates by extracting CorrelationId from each message:
Event(() => StockWasReserved, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

// Each consumer must echo the CorrelationId from the incoming command onto the reply event:
await context.Publish(new StockReservedEvent(
    msg.CorrelationId,   // ← echo through
    msg.OrderId, reservationId, DateTime.UtcNow));
```

---

## 8. Checklist — Adding a New Saga Step

- [ ] Add `State` property for the new waiting state
- [ ] Add `Event<T>` properties for success and failure events
- [ ] Add `Event(() => ..., x => x.CorrelateById(...))` registrations in the constructor
- [ ] Update the previous terminal transition to `TransitionTo(NewState)` instead of `Completed`/`Finalize`
- [ ] Add `During(NewState, When(Success)..., When(Failure)...)` block
- [ ] Add compensation `PublishAsync` calls on the failure path (reverse order of steps)
- [ ] Add any new resource-ID properties to `OrderSagaState`
- [ ] Add integration event contracts to the relevant `IntegrationEvents` project
- [ ] Add consumer in the target module (see `integration-events-consumers` skill)
- [ ] Run EF migration if `OrderSagaState` schema changed
