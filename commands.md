# Infrastructure

```bash
docker compose -f docker-compose.yml up -d
```

```bash
docker compose -f docker-compose.yml up
```

```bash
docker compose -f docker-compose.yml down
```

# User Secrets

```bash
# Initialize user secrets for the API project
dotnet user-secrets init --project src/Api/ModularMonolithEventDriven.Api
dotnet user-secrets init --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure
dotnet user-secrets init --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure
dotnet user-secrets init --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure
dotnet user-secrets init --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure
```

```bash
# Set a user secret (example: connection string)
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" "your-connection-string" --project src/Api/ModularMonolithEventDriven.Api
```

```bash
# List all user secrets
dotnet user-secrets list --project src/Api/ModularMonolithEventDriven.Api
```

```bash
# Remove a specific user secret
dotnet user-secrets remove "key-name" --project src/Api/ModularMonolithEventDriven.Api
```

```bash
# Clear all user secrets
dotnet user-secrets clear --project src/Api/ModularMonolithEventDriven.Api
```

# User Secrets — Infrastructure Projects

## Orders

```bash
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" "your-connection-string" --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure
```

## Inventory

```bash
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" "your-connection-string" --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure
```

## Payments

```bash
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" "your-connection-string" --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure
```

## Notifications

```bash
dotnet user-secrets set "ConnectionStrings:ModularMonolithEventDrivenDb" "your-connection-string" --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure
```

# Orders

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context OrdersDbContext
```

```bash
# Apply migrations
dotnet ef database update --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure  --startup-project src/Api/ModularMonolithEventDriven.Api  --context OrdersDbContext
```

# Inventory

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context InventoryDbContext
```

```bash
# Apply migrations
dotnet ef database update --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context InventoryDbContext
```

# Payments

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context PaymentsDbContext
```

```bash
# Apply migrations
dotnet ef database update --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context PaymentsDbContext
```

# Notifications

```bash
# Add a new migration
dotnet ef migrations add MigrationName --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context NotificationsDbContext
```

```bash
# Apply migrations
dotnet ef database update --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context NotificationsDbContext
```