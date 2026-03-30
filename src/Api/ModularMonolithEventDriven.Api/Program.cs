using MassTransit;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;
using ModularMonolithEventDriven.Modules.Orders.Presentation;
using ModularMonolithEventDriven.Modules.Inventory.Presentation;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Extensions;
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
    // Each module reacts independently to OrderCancelledEvent (no central coordinator)
    x.AddConsumer<OrderCancelledInventoryConsumer>();   // Inventory: releases reserved stock
    x.AddConsumer<OrderCancelledPaymentConsumer>();     // Payments: refunds payment if exists
    x.AddConsumer<OrderCancelledNotificationConsumer>(); // Notifications: sends cancellation confirmation

    // --- Orchestration Consumers ---
    // Saga issues commands; each consumer handles one step
    x.AddConsumer<ReserveStockCommandConsumer>();
    x.AddConsumer<ReleaseStockCommandConsumer>();
    x.AddConsumer<ProcessPaymentCommandConsumer>();
    x.AddConsumer<SendOrderNotificationConsumer>();

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
