---
name: modular-monolith-architecture
description: This skill should be used when the user asks about "modular monolith architecture", "choreography vs orchestration", "saga pattern", "module structure", "how modules communicate", "bounded context", "when to use this architecture", "layer dependencies", "module DI registration", or asks for an architectural overview of this project.
version: 0.1.0
---

# Modular Monolith Architecture (.NET 9)

This project is a .NET 9 Modular Monolith demonstrating two distributed transaction patterns side-by-side.

## Core Design

A **Modular Monolith** is a single deployable unit divided into strongly-bounded modules. Modules communicate via **integration events** over RabbitMQ (async, via MassTransit) — never via direct method calls or shared DbContexts. This enforces loose coupling while keeping operational simplicity (single deploy, single DB).

## Two Patterns Demonstrated

| Pattern | Endpoint | Coordinator |
|---|---|---|
| **Choreography** | `POST /api/orders/choreography` | None — modules react to events autonomously |
| **Orchestration** | `POST /api/orders/orchestration` | `OrderSaga` state machine coordinates every step |

## Module Structure (5 projects per module)

```
Ochestrator.Modules.<Name>.Domain          → Entities, Repository interfaces, Domain errors
Ochestrator.Modules.<Name>.Application     → Commands, Queries, MediatR Handlers, IUnitOfWork
Ochestrator.Modules.<Name>.Infrastructure  → DbContext, Repos, MassTransit Consumers, DI ext
Ochestrator.Modules.<Name>.IntegrationEvents → Event/Command contracts for RabbitMQ
Ochestrator.Modules.<Name>.Presentation    → Minimal API endpoints
```

**Modules:** Orders · Inventory · Payments · Notifications

## Key Design Decisions

| Decision | Detail |
|---|---|
| **Schema-per-module** | Single SQL Server DB; each module owns a schema (`orders`, `inventory`, `payments`, `notifications`) |
| **Module-specific IUnitOfWork** | `IOrdersUnitOfWork`, `IInventoryUnitOfWork`, etc. prevent DI conflicts across modules |
| **Auto-migrations** | `Program.cs` calls `Database.MigrateAsync()` on all 4 DbContexts at startup |
| **CQRS** | Commands return `Result<T>`; queries return `Result<T>`; MediatR pipeline: logging → validation |
| **Saga persistence** | `orders.OrderSagaState` table, optimistic concurrency via `MassTransit.EntityFrameworkCore` |

## When to Use This Architecture

- Teams of 3–15 devs where modules map to team/domain boundaries
- Need clean separation without microservices operational complexity
- Planning to potentially extract modules to services later (integration events = already the API contract)

## Deep Dive

Load `references/architecture-deep-dive.md` for:
- Full layer dependency rules
- DI registration pattern (per-module extension methods)
- MassTransit consumer pattern
- Choreography flow step-by-step
- Orchestration/Saga flow with compensations
- CQRS abstractions reference
- Technology stack table with rationale

## Key Files

- [Program.cs](src/Api/Ochestrator.Api/Program.cs) — module wiring, MassTransit config, migrations
- [OrderSaga.cs](src/Modules/Orders/Ochestrator.Modules.Orders.Application/Saga/OrderSaga.cs) — Saga state machine
- [Directory.Packages.props](Directory.Packages.props) — centralized NuGet versions
