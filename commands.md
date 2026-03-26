# Infrastructure
docker compose -f docker-compose up -d

docker compose -f docker-compose up

docker compose -f docker-compose.yml down

# Orders
dotnet ef database update --project src/Modules/Orders/ModularMonolithEventDriven.Modules.Orders.Infrastructure  --startup-project src/Api/ModularMonolithEventDriven.Api  --context OrdersDbContext

# Inventory
dotnet ef database update --project src/Modules/Inventory/ModularMonolithEventDriven.Modules.Inventory.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context InventoryDbContext

# Payments
dotnet ef database update --project src/Modules/Payments/ModularMonolithEventDriven.Modules.Payments.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context PaymentsDbContext

# Notifications
dotnet ef database update --project src/Modules/Notifications/ModularMonolithEventDriven.Modules.Notifications.Infrastructure --startup-project src/Api/ModularMonolithEventDriven.Api --context NotificationsDbContext