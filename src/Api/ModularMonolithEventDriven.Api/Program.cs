using MassTransit;
using Microsoft.EntityFrameworkCore;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.Presentation;
using ModularMonolithEventDriven.Modules.Inventory.Presentation;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Payments.Presentation;
using ModularMonolithEventDriven.Modules.Notifications.Presentation;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Module registrations
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

// MassTransit
builder.Services.AddMassTransit(x =>
{
    // --- Choreography Consumers ---
    // Inventory: reacts to OrderPlacedEvent
    x.AddConsumer<OrderPlacedInventoryConsumer>();
    x.AddConsumer<ReserveStockCommandConsumer>();
    x.AddConsumer<ReleaseStockCommandConsumer>();

    // Payments: reacts to StockReservedEvent (choreography) + ProcessPaymentCommand (orchestration)
    x.AddConsumer<StockReservedPaymentConsumer>();
    x.AddConsumer<ProcessPaymentCommandConsumer>();

    // Notifications: reacts to PaymentProcessedEvent (choreography) + SendOrderNotificationCommand (orchestration)
    x.AddConsumer<PaymentProcessedNotificationConsumer>();
    x.AddConsumer<SendOrderNotificationConsumer>();

    // Orders: update order status from choreography events
    x.AddConsumer<StockReservedOrderUpdater>();
    x.AddConsumer<PaymentProcessedOrderUpdater>();
    x.AddConsumer<PaymentFailedOrderUpdater>();

    // --- Orchestration: Saga State Machine ---
    x.AddSagaStateMachine<OrderSaga, OrderSagaState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<OrdersDbContext>();
        });

    x.UsingRabbitMq((ctx, cfg) =>
    {
        var rabbitMqSection = builder.Configuration.GetSection("RabbitMQ");
        cfg.Host(rabbitMqSection["Host"] ?? "localhost", "/", h =>
        {
            h.Username(rabbitMqSection["Username"] ?? "guest");
            h.Password(rabbitMqSection["Password"] ?? "guest");
        });

        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1)));

        cfg.ConfigureEndpoints(ctx);
    });
});

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations automatically on startup
await ApplyMigrationsAsync(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "ModularMonolithEventDriven API";
        options.Theme = ScalarTheme.Purple;
    });
}

app.UseSerilogRequestLogging();

// Map module endpoints
app.MapOrdersEndpoints();
app.MapInventoryEndpoints();
app.MapPaymentsEndpoints();
app.MapNotificationsEndpoints();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations.");
        throw;
    }
}
