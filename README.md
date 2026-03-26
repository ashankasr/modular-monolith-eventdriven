# ModularMonolithEventDriven

A **.NET 9 Modular Monolith** demonstrating two distributed transaction patterns:

| Pattern | Endpoint | Description |
|---|---|---|
| **Choreography** | `POST /api/orders/choreography` | Modules react to events autonomously |
| **Orchestration** | `POST /api/orders/orchestration` | Saga state machine coordinates every step with compensation |

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 9.0+ | `dotnet --version` to verify |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | Latest | Required for RabbitMQ |
| SQL Server | 2019+ | Local instance (Windows Auth or SA login) |
| [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) | Latest | `dotnet tool install --global dotnet-ef` |

---

## 1. Clone & Restore

```bash
git clone <repo-url>
cd ochestrator
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

### Seed inventory first

```http
POST /api/inventory/products
Content-Type: application/json

{
  "name": "Widget",
  "price": 9.99,
  "stock": 100
}
```

### Place an order (Choreography)

```http
POST /api/orders/choreography
Content-Type: application/json

{
  "productId": "<product-id>",
  "quantity": 1
}
```

### Place an order (Orchestration / Saga)

```http
POST /api/orders/orchestration
Content-Type: application/json

{
  "productId": "<product-id>",
  "quantity": 1
}
```

### Other endpoints

```
GET /api/inventory/products
GET /api/orders/{orderId}
GET /api/payments
GET /api/notifications
```

---

## 6. EF Migrations (manual)

Migrations are auto-applied on startup, but you can run them manually or add new ones.

### Add a migration

```bash
# Orders
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
  --context OrdersDbContext

# Inventory
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
  --context InventoryDbContext

# Payments
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
  --context PaymentsDbContext

# Notifications
dotnet ef migrations add <MigrationName> \
  --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
  --context NotificationsDbContext
```

### Apply migrations manually

```bash
dotnet ef database update \
  --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure \
  --startup-project src/Api/ModularMonolithEventDriven.Api \
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
│       ├── *.Domain                            # Entities, Value Objects
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

## Troubleshooting

| Issue | Fix |
|---|---|
| `Cannot open database` | Verify SQL Server is running and the connection string in user secrets is correct |
| `Connection refused` on RabbitMQ | Run `docker compose up -d` and wait for the health check to pass |
| Migrations not found | Ensure EF Core CLI is installed: `dotnet tool install --global dotnet-ef` |
| Port already in use | Check `launchSettings.json` or set `ASPNETCORE_URLS` env var |
