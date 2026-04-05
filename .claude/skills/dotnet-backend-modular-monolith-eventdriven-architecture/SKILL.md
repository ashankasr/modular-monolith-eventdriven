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
