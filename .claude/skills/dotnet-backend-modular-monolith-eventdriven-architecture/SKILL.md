---
name: modular-monolith-architecture
description: This skill should be used when the user asks about "modular monolith architecture", "choreography vs orchestration", "saga pattern", "module structure", "how modules communicate", "bounded context", "when to use this architecture", "layer dependencies", "module DI registration", or asks for an architectural overview of this project.
---

# Modular Monolith Architecture (.NET)

A **Modular Monolith** is a single deployable unit divided into strongly-bounded modules. Modules communicate via **integration events** (async, via a message broker) — never via direct method calls or shared DbContexts. This enforces loose coupling while keeping operational simplicity (single deploy, single DB).

## Module Structure (5 projects per module)

```
<Solution>.Modules.<Name>.Domain           → Entities, Repository interfaces, Domain errors
<Solution>.Modules.<Name>.Application      → Commands, Queries, MediatR Handlers, IUnitOfWork
<Solution>.Modules.<Name>.Infrastructure   → DbContext, Repos, Message Consumers, DI extensions
<Solution>.Modules.<Name>.IntegrationEvents → Event/Command contracts published to the broker
<Solution>.Modules.<Name>.Presentation     → Minimal API endpoints
```

## Key Design Decisions

| Decision | Detail |
|---|---|
| **Schema-per-module** | Single DB; each module owns its schema — no cross-module table joins |
| **Module-specific IUnitOfWork** | Each module declares its own `I<Module>UnitOfWork` to prevent DI conflicts |
| **Auto-migrations** | Host calls `Database.MigrateAsync()` on each DbContext at startup |
| **CQRS via MediatR** | Commands/Queries return `Result<T>`; pipeline behaviors handle logging and validation |
| **Saga persistence** | Saga state stored in DB with optimistic concurrency (MassTransit.EntityFrameworkCore) |
| **Transactional Outbox** | Domain events written to `OutboxMessage` rows in the same DB transaction as the aggregate save; a background processor dispatches them via MediatR to `IDomainEventHandler<T>` |

## Distributed Transaction Patterns

### Choreography
Modules react to events autonomously — no central coordinator. Each module publishes an event on success; downstream modules subscribe and continue the flow. Simple to add new steps but harder to trace end-to-end.

### Orchestration (Saga)
A central state machine (`OrderSaga` / equivalent) sends commands to each module in sequence and handles compensating transactions on failure. Explicit control flow; easier to reason about failures and rollbacks.

## Layer Dependency Rules

```
Presentation → Application → Domain        (always inward)
Infrastructure → Application, Domain       (implements interfaces defined in inner layers)
IntegrationEvents                          (referenced by Infrastructure only — no business logic)
```

Cross-module dependencies are forbidden. Only integration event contracts may be shared.

## DI Registration Pattern

Each module's Infrastructure project contains a `<Module>Module.cs` static class with two responsibilities:

**1. Service registration** — wires up DbContext, repositories, and MediatR:
```csharp
// Infrastructure/Extensions/<Module>Module.cs
public static class <Module>Module
{
    public static IServiceCollection Add<Module>Module(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<<Module>DbContext>(...);
        services.AddScoped<I<Module>Repository, <Module>Repository>();
        services.AddScoped<I<Module>UnitOfWork>(sp => sp.GetRequiredService<<Module>DbContext>());
        services.AddApplication(typeof(Application.AssemblyReference).Assembly);
        return services;
    }
}
```

**2. Consumer registration** — exposes a static method matching `Action<IRegistrationConfigurator>`:
```csharp
    public static void ConfigureConsumers(IRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<SomeEventConsumer>();
        configurator.AddConsumer<SomeCommandConsumer>();
        // Saga state machines also registered here (Orders module)
    }
```

## Infrastructure Wiring (host Program.cs)

Module service registrations and broker setup are separate concerns:

```csharp
// 1. Register each module's services
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

// 2. Register MassTransit + RabbitMQ via Common.Infrastructure
builder.Services.AddInfrastructure(
    [
        InventoryModule.ConfigureConsumers,
        PaymentsModule.ConfigureConsumers,
        NotificationsModule.ConfigureConsumers,
        OrdersModule.ConfigureConsumers,
    ],
    builder.Configuration);
```

`AddInfrastructure` lives in `Common.Infrastructure` and handles `AddMassTransit`, endpoint naming, retry policy, and RabbitMQ host config (reads `ConnectionStrings:RabbitMQ` as an AMQP URI).

## Endpoint Registration Pattern

All Minimal API endpoints are mapped via a single call in `Program.cs`:

```csharp
app.MapEndpoints();
```

`MapEndpoints()` is defined in `src/Api/*/Extensions/WebApplicationExtensions.cs` (internal to the API host) and calls each module's `Map<Module>Endpoints()` extension. The API host is the only place that knows about all Presentation projects — Common.Infrastructure does not.

## Transactional Outbox Pattern

### The dual-write problem

Calling `publishEndpoint.Publish(...)` directly in a command handler after `SaveChangesAsync()` is a **dual-write**: if the broker publish fails, the DB row exists but no message was sent — the event is silently lost with no retry.

### How the outbox solves it

```
unitOfWork.SaveChangesAsync()
  └─ OutboxMessagesInterceptor (EF SaveChanges interceptor)
       → writes domain events as OutboxMessage rows in the SAME transaction
         ↓ (background, Quartz job every 30 s)
OutboxMessageProcessor<TDbContext>
  → reads rows WHERE ProcessedOnUtc IS NULL
  → deserialises to IDomainEvent
  → publisher.Publish(DomainEventNotification<T>)  [MediatR]
    ↓
IDomainEventHandler<T>
  → publishes integration event / MassTransit message to RabbitMQ
  → marks row ProcessedOnUtc = UtcNow
```

### Outbox processor error handling — critical rule

The `OutboxMessageProcessor` catch block must **never** set `ProcessedOnUtc` on failure. Only `Error` should be set. Rows with `ProcessedOnUtc == null` are retried on every poll cycle; rows with `ProcessedOnUtc` set are permanently skipped.

```csharp
// CORRECT — transient failures (e.g. RabbitMQ down) are retried automatically
catch (Exception ex)
{
    logger.LogError(ex, "Outbox: failed to process message {Id}", message.Id);
    message.Error = ex.ToString();
    // ProcessedOnUtc intentionally NOT set
}
```

### Where to publish integration messages

Do **not** call `publishEndpoint.Publish(...)` directly in command handlers. Instead:

1. The entity raises a domain event in its factory / state-change method (`RaiseDomainEvent(...)`)
2. The outbox interceptor captures it atomically with the `SaveChangesAsync`
3. `IDomainEventHandler<T>` receives it (via outbox processor) and publishes the MassTransit message

This separates *persistence* (command handler) from *reaction* (event handler), and the outbox provides the delivery guarantee.

---

## When to Use This Architecture

- Teams of 3–15 devs where modules map to domain/team boundaries
- Need clean separation without microservices operational complexity
- Planning to potentially extract modules to services later (integration events already serve as the API contract)

## Key Files to Check in Any Repo Using This Pattern

| File | Purpose |
|---|---|
| `src/Api/*/Program.cs` | Module wiring, broker config, auto-migrations |
| `src/Api/*/Extensions/WebApplicationExtensions.cs` | `MapEndpoints()` — centralises all route registration |
| `src/Common/*/Infrastructure/Extensions/InfrastructureExtensions.cs` | `AddInfrastructure()` — MassTransit + RabbitMQ setup |
| `src/Modules/*/Infrastructure/Extensions/<Module>Module.cs` | `Add<Module>Module()` + `ConfigureConsumers()` |
| `src/Modules/*/Application/Saga/*.cs` | Saga state machine (orchestration) |
| `src/Modules/*/Infrastructure/Consumers/` | Event consumers (choreography) |
| `Directory.Packages.props` | Centralized NuGet versions |
| `docker-compose.yml` | Infrastructure services (broker, DB) |
