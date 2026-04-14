# Ochestrator — Claude Code Guide

## Purpose

This project is a **blueprint** for building production-grade **.NET 10 Modular Monolith** applications with event-driven architecture. It demonstrates two distributed transaction patterns side-by-side so developers can learn when and how to apply each:

Treat every design decision here as an intentional teaching example, not just implementation code.

---

## Architecture

### Clean Architecture + DDD — per module

```
Ochestrator.Modules.<Name>.Domain          → Entities, Value Objects, Domain Events
Ochestrator.Modules.<Name>.Application     → Commands, Queries, MediatR Handlers
Ochestrator.Modules.<Name>.Infrastructure  → DbContext, Repositories, Consumers, EF Migrations
Ochestrator.Modules.<Name>.IntegrationEvents → Contracts published to/consumed from RabbitMQ
Ochestrator.Modules.<Name>.Presentation    → Minimal API endpoint mappings
```

**Modules:** Orders · Inventory · Payments · Notifications

### Common (shared kernel)

```
Ochestrator.Common.Domain          → Base entities, Result<T>/Error primitives
Ochestrator.Common.Application     → MediatR pipeline behaviors, AddApplication() extension
Ochestrator.Common.Infrastructure  → BaseDbContext (audit fields, IUnitOfWork base)
```

---

## Key Design Decisions

These are architectural constraints — apply them consistently when adding or modifying code:

- **Module-specific IUnitOfWork**: `IOrdersUnitOfWork`, `IInventoryUnitOfWork`, `IPaymentsUnitOfWork` are registered separately to avoid DI conflicts. Notifications has no UoW.
- **Schema-per-module**: Single SQL Server DB with `orders`, `inventory`, `payments`, `notifications` schemas — modules own their data.
- **No cross-module direct calls**: Modules communicate only via integration events over RabbitMQ (MassTransit). No shared DbContext, no direct service references across modules.
- **Saga state in DB**: `orders.OrderSagaState` via `MassTransit.EntityFrameworkCore` with optimistic concurrency.
- **CQRS via MediatR**: Commands and queries live in the Application layer. Each module self-registers via `AddApplication(assembly)`.
- **Auto-migrations on startup**: `Program.cs` calls `Database.MigrateAsync()` on all 4 DbContexts.
- **MassTransit retry**: Exponential, 5 attempts, 1s → 5min interval, 5s increment.

---

## Technology Stack

| Concern | Library | Version |
|---|---|---|
| Message broker | MassTransit + RabbitMQ | 8.3.6 |
| Saga persistence | MassTransit.EntityFrameworkCore | 8.3.6 |
| ORM | Entity Framework Core (SQL Server) | 10.0.0 |
| CQRS | MediatR | 12.4.1 |
| Validation | FluentValidation | 12.1.1 |
| Mapping | Mapster | 10.0.7 |
| Logging | Microsoft.Extensions.Logging + OpenTelemetry | 10.0.0 / 1.12.0 |
| API docs | Scalar.AspNetCore | 2.1.7 |

NuGet versions are centrally managed in `Directory.Packages.props`.

---

## Infrastructure

Only **RabbitMQ** runs in Docker. SQL Server runs locally.

```bash
docker-compose up -d   # starts RabbitMQ (5672 / management UI 15672)
```

---

## Skills Available

Implementation guidance is provided via Claude skills — use them for specific tasks:

| Skill | When to use |
|---|---|
| `dotnet-backend-modular-monolith-implement-feature` | Implementing a feature end-to-end across all layers |
| `dotnet-backend-modular-monolith-cqrs-patterns` | Adding commands, queries, handlers, validation |
| `dotnet-backend-modular-monolith-domain-ef` | Adding entities, EF config (`IEntityTypeConfiguration<T>`), migrations |
| `dotnet-backend-modular-monolith-eventdriven-architecture` | Architecture questions, module boundaries |
| `dotnet-backend-modular-monolith-eventdriven-create-module` | Scaffolding a new module |
| `dotnet-backend-modular-monolith-integration-events-consumers` | Adding consumers, integration events |
| `dotnet-backend-modular-monolith-presentation-endpoints` | Adding HTTP endpoints |
| `dotnet-backend-modular-monolith-saga` | Extending the OrderSaga, adding saga steps |


## Self-Updating Skills Engine

1. When the user corrects you or you make a mistake, update the relevant skill.
2. When the user adds a new skill, add it into the **## Skills Available** table.
3. When a project-wide pattern changes, update **all** skills that reference the old pattern — not just the one most directly related.
4. When a new architectural constraint is established, add it to **## Key Design Decisions**.