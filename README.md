# Modular Monolith — .NET 10 Blueprint

A **production-grade blueprint** for building **.NET 10 Modular Monolith** applications with event-driven architecture and CQRS.

This repo is a learning reference — every design decision is intentional and documented so you can understand *why*, not just *how*.

---

## What This Demonstrates

| Concern | Approach |
|---|---|
| **Module isolation** | Clean Architecture per module — Domain, Application, Infrastructure, Presentation |
| **Inter-module communication** | Integration events over RabbitMQ (MassTransit) — no direct references between modules |
| **Distributed transactions** | Two patterns side-by-side: Orchestration (Saga) and Choreography |
| **CQRS** | Commands and queries via MediatR with FluentValidation pipeline |
| **Data isolation** | Single SQL Server DB, schema-per-module (`orders`, `inventory`, `payments`, `notifications`) |
| **Saga persistence** | MassTransit state machine persisted to DB with optimistic concurrency |

### Transaction Patterns

| Pattern | Endpoint | Description |
|---|---|---|
| **Orchestration** | `POST /api/orders` | Saga state machine coordinates every step (ReserveStock → ProcessPayment → Notify) with compensation on failure |
| **Choreography** | `POST /api/orders/{id}/cancel` | `OrderCancelledEvent` published — each module reacts autonomously with no central coordinator |

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | `dotnet --version` to verify |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | Latest | Required for RabbitMQ |
| SQL Server | 2019+ | Local instance (Windows Auth or SA login) |
| [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) | Latest | `dotnet tool install --global dotnet-ef` |

---

## 1. Clone & Restore

```bash
git clone <repo-url>
cd <repo-name>
dotnet restore
```

---

## 2. Start Infrastructure (Docker)

Only **RabbitMQ** runs in Docker. SQL Server is your local instance.

```bash
docker compose -f docker-compose.yml up -d
```

Verify RabbitMQ is healthy:

```
RabbitMQ Management UI: http://localhost:15672
Credentials: guest / guest
```

To stop:

```bash
docker compose -f docker-compose.yml down
```

---

## 3. Configure User Secrets

The connection string is kept out of source control using [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets).

### 3a. Initialize user secrets

Run once per project (only needed on first setup):

```bash
dotnet user-secrets init --project src/Api/ModularMonolithEventDriven.Api
dotnet user-secrets init --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure
dotnet user-secrets init --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure
dotnet user-secrets init --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure
dotnet user-secrets init --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure
```

### 3b. Set the connection string

Replace the connection string below with your local SQL Server credentials.

**API project** (used at runtime):

```bash
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" \
  "Server=localhost;Integrated Security=false;User ID=sa;Password=yourpassword;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb" \
  --project src/Api/ModularMonolithEventDriven.Api
```

**Infrastructure projects** (used when running EF CLI migrations):

```bash
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" \
  "Server=localhost;Integrated Security=false;User ID=sa;Password=yourpassword;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb" \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure

dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" \
  "Server=localhost;Integrated Security=false;User ID=sa;Password=yourpassword;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb" \
  --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure

dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" \
  "Server=localhost;Integrated Security=false;User ID=sa;Password=yourpassword;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb" \
  --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure

dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" \
  "Server=localhost;Integrated Security=false;User ID=sa;Password=yourpassword;TrustServerCertificate=true;Initial Catalog=ModularMonolithEventDrivenDb" \
  --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure
```

> **Windows Authentication?** Use `Integrated Security=true` and omit `User ID`/`Password`.

### 3c. Verify secrets are set

```bash
dotnet user-secrets list --project src/Api/ModularMonolithEventDriven.Api
```

---

## 4. Run the Application

Migrations are applied automatically on startup.

```bash
dotnet run --project src/Api/ModularMonolithEventDriven.Api
```

Once running:

- **Scalar API UI**: `http://localhost:<PORT>/scalar/v1`
- **Swagger/OpenAPI**: `http://localhost:<PORT>/openapi/v1.json`

---

## 5. Try the API

### 1. Seed inventory

```http
POST /api/inventory/products
Content-Type: application/json

{
  "name": "Widget",
  "sku": "WIDGET-001",
  "stockQuantity": 100,
  "price": 9.99
}
```

Copy the returned `id` — you'll need it as `productId` below.

### 2. Place an order (Orchestration / Saga)

```http
POST /api/orders
Content-Type: application/json

{
  "customerId": "customer-123",
  "customerEmail": "customer@example.com",
  "items": [
    {
      "productId": "<product-id>",
      "productName": "Widget",
      "quantity": 2,
      "unitPrice": 9.99
    }
  ]
}
```

Watch the logs — the saga coordinates: `ReserveStock → ProcessPayment → Notify`. Try with an unknown `productId` to see compensation fire.

### 3. Cancel an order (Choreography)

```http
POST /api/orders/{orderId}/cancel
Content-Type: application/json

{
  "reason": "Changed my mind"
}
```

`OrderCancelledEvent` is published. Each module reacts independently: Inventory releases stock, Payments refunds, Notifications sends a cancellation email.

### Other endpoints

```
GET /api/inventory/products
GET /api/orders/{orderId}
```

---

## 6. EF Migrations (manual)

Migrations are auto-applied on startup, but you can run them manually or add new ones.

### Add a migration

Run from the repo root. Use full `.csproj` paths:

```bash
# Orders
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure/ModularMonolithEventDriven.Modules.Orders.Infrastructure.csproj \
  --startup-project src/Api/ModularMonolithEventDriven.Api/ModularMonolithEventDriven.Api.csproj \
  --context OrdersDbContext

# Inventory
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure/ModularMonolithEventDriven.Modules.Inventory.Infrastructure.csproj \
  --startup-project src/Api/ModularMonolithEventDriven.Api/ModularMonolithEventDriven.Api.csproj \
  --context InventoryDbContext

# Payments
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure/ModularMonolithEventDriven.Modules.Payments.Infrastructure.csproj \
  --startup-project src/Api/ModularMonolithEventDriven.Api/ModularMonolithEventDriven.Api.csproj \
  --context PaymentsDbContext

# Notifications
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure/ModularMonolithEventDriven.Modules.Notifications.Infrastructure.csproj \
  --startup-project src/Api/ModularMonolithEventDriven.Api/ModularMonolithEventDriven.Api.csproj \
  --context NotificationsDbContext
```

### Apply migrations manually

```bash
dotnet ef database update \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure/ModularMonolithEventDriven.Modules.Orders.Infrastructure.csproj \
  --startup-project src/Api/ModularMonolithEventDriven.Api/ModularMonolithEventDriven.Api.csproj \
  --context OrdersDbContext
```

_(Repeat for Inventory, Payments, Notifications using their respective context names.)_

---

## Project Structure

```
src/
├── Api/
│   └── ModularMonolithEventDriven.Api/        # Entry point — wires all modules
├── Modules/
│   ├── Orders/
│   ├── Inventory/
│   ├── Payments/
│   └── Notifications/
│       ├── *.Domain                            # Entities, Value Objects, Domain Events
│       ├── *.Application                       # Commands, Queries, MediatR handlers
│       ├── *.Infrastructure                    # DbContext, Repositories, EF Migrations
│       ├── *.IntegrationEvents                 # RabbitMQ message contracts
│       └── *.Presentation                      # Minimal API endpoint mappings
└── Common/
    ├── ModularMonolithEventDriven.Common.Domain
    ├── ModularMonolithEventDriven.Common.Application
    └── ModularMonolithEventDriven.Common.Infrastructure
```

---

## Technology Stack

| Concern | Library | Version |
|---|---|---|
| Message broker | MassTransit + RabbitMQ | 9.1.0 |
| Saga persistence | MassTransit.EntityFrameworkCore | 9.1.0 |
| ORM | Entity Framework Core (SQL Server) | 10.0.0 |
| CQRS | MediatR | 12.4.1 |
| Validation | FluentValidation | 12.1.1 |
| Mapping | Mapster | 10.0.7 |
| Logging | Serilog.AspNetCore | 10.0.0 |
| API docs | Scalar.AspNetCore | 2.1.7 |

---

## Troubleshooting

| Issue | Fix |
|---|---|
| `Cannot open database` | Verify SQL Server is running and the connection string in user secrets is correct |
| `Connection refused` on RabbitMQ | Run `docker compose up -d` and wait for the health check to pass |
| Migrations not found | Ensure EF Core CLI is installed: `dotnet tool install --global dotnet-ef` — verify with `dotnet ef --version` |
| Port already in use | Check `launchSettings.json` or set `ASPNETCORE_URLS` env var |
