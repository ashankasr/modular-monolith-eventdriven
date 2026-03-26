# Ochestrator — Claude Code Guide

## What This Project Is

A **.NET 9 Modular Monolith** demonstrating two distributed transaction patterns side-by-side:

| Pattern | Trigger | How it works |
|---|---|---|
| **Choreography** | `POST /api/orders/choreography` | Modules react to events autonomously (no central coordinator) |
| **Orchestration** | `POST /api/orders/orchestration` | `OrderSaga` state machine coordinates every step with compensation |

---

## Architecture

### Clean Architecture + DDD — per module

Each of the 4 modules follows this layer structure:

```
Ochestrator.Modules.<Name>.Domain          → Entities, Value Objects, Domain Events
Ochestrator.Modules.<Name>.Application     → Commands, Queries, MediatR Handlers
Ochestrator.Modules.<Name>.Infrastructure  → DbContext, Repositories, Consumers, EF Migrations
Ochestrator.Modules.<Name>.IntegrationEvents → Contracts published to/consumed from RabbitMQ
Ochestrator.Modules.<Name>.Presentation    → Minimal API endpoint mappings
```

**Modules:** Orders · Inventory · Payments · Notifications

### Common (shared across all modules)

```
Ochestrator.Common.Domain          → Base entities, Result<T>/Error primitives
Ochestrator.Common.Application     → MediatR pipeline behaviors, AddApplication() extension
Ochestrator.Common.Infrastructure  → BaseDbContext (audit fields, IUnitOfWork base)
```

### API Host

```
src/Api/Ochestrator.Api/Program.cs  → Module wiring, MassTransit config, auto-migrations, endpoint mapping
```

---

## Key Design Decisions

- **Module-specific IUnitOfWork**: `IOrdersUnitOfWork`, `IInventoryUnitOfWork`, `IPaymentsUnitOfWork` registered separately to avoid DI conflicts. Notifications has no UoW interface.
- **Single SQL Server DB, schema-per-module**: `orders`, `inventory`, `payments`, `notifications` schemas.
- **Saga state** persisted in `orders.OrderSagaState` via `MassTransit.EntityFrameworkCore` with optimistic concurrency.
- **Auto-migrations on startup**: `Program.cs` calls `Database.MigrateAsync()` on all 4 DbContexts.
- **CQRS via MediatR**: Commands/Queries in Application layer; each module registers its own handlers via `AddApplication(assembly)`.
- **MassTransit retry**: 3 attempts at 100ms → 500ms → 1s intervals.

---

## Technology Stack

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

## Infrastructure

Only **RabbitMQ** runs in Docker. SQL Server is the local instance.

```bash
# Start RabbitMQ only
docker-compose up -d

# RabbitMQ Management UI
http://localhost:15672  (guest / guest)
```

**Connection string** (`appsettings.json`):
```
Server=localhost;Integrated Security=false;User ID=sa;Password=password;TrustServerCertificate=true;Initial Catalog=OchestratorDb
```

---

## Running the App

```bash
# 1. Start RabbitMQ
docker-compose up -d

# 2. Run the API (auto-applies all EF migrations on startup)
dotnet run --project src/Api/Ochestrator.Api

# Scalar API UI available at: http://localhost:<PORT>/scalar/v1
```

---

## API Endpoints

### Inventory (seed first)
```
POST /api/inventory/products       → Create a product with stock
GET  /api/inventory/products       → List products
```

### Orders
```
POST /api/orders/choreography      → Place order via event choreography
POST /api/orders/orchestration     → Place order via saga orchestration
GET  /api/orders/{orderId}         → Get order status
```

### Payments / Notifications
```
GET /api/payments
GET /api/notifications
```

---

## EF Migrations

Migrations live inside each module's `Infrastructure` project. To add a new migration:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/Ochestrator.Modules.<Module>.Infrastructure \
  --startup-project src/Modules/<Module>/Ochestrator.Modules.<Module>.Infrastructure \
  --context <Module>DbContext

# Apply manually (app also does this on startup)
dotnet ef database update \
  --project src/Modules/<Module>/Ochestrator.Modules.<Module>.Infrastructure \
  --startup-project src/Modules/<Module>/Ochestrator.Modules.<Module>.Infrastructure \
  --context <Module>DbContext
```

---

## Key Files

| File | Purpose |
|---|---|
| [Program.cs](src/Api/Ochestrator.Api/Program.cs) | Module wiring, MassTransit, auto-migrations |
| [OrderSaga.cs](src/Modules/Orders/Ochestrator.Modules.Orders.Application/Saga/OrderSaga.cs) | Orchestration state machine |
| [OrdersEndpoints.cs](src/Modules/Orders/Ochestrator.Modules.Orders.Presentation/OrdersEndpoints.cs) | HTTP endpoints |
| [docker-compose.yml](docker-compose.yml) | RabbitMQ container |
| [Directory.Packages.props](Directory.Packages.props) | Centralized NuGet versions |
| [appsettings.json](src/Api/Ochestrator.Api/appsettings.json) | Connection strings, RabbitMQ, logging |

---

## Saga Flow (Orchestration)

```
POST /api/orders/orchestration
  → StartOrderSagaCommand
    → [Saga] ReserveStockCommand → Inventory
      ↳ StockWasReserved → [Saga] ProcessPaymentCommand → Payments
          ↳ PaymentWasProcessed → [Saga] SendOrderNotificationCommand → Notifications → ✅ Done
          ↳ PaymentFailed → [Saga] ReleaseStockCommand (compensation) + Notification → ❌ Failed
      ↳ StockReservationFailed → [Saga] SendOrderNotificationCommand → ❌ Failed
```

## Choreography Flow

```
POST /api/orders/choreography
  → PlaceOrderCommand → Order created, publishes OrderPlacedEvent
    → Inventory consumer: reserves stock, publishes StockReservedEvent
      → Payments consumer: processes payment, publishes PaymentProcessedEvent
        → Notifications consumer: sends notification
```
