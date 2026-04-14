using ModularMonolithEventDriven.Api.Extensions;
using ModularMonolithEventDriven.Common.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Extensions;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Extensions;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OTel, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Module registrations
builder.Services.AddOrdersModule(builder.Configuration);
builder.Services.AddInventoryModule(builder.Configuration);
builder.Services.AddPaymentsModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

// Infrastructure (MassTransit + RabbitMQ)
builder.Services.AddInfrastructure(
    [
        InventoryModule.ConfigureConsumers,
        PaymentsModule.ConfigureConsumers,
        NotificationsModule.ConfigureConsumers,
        OrdersModule.ConfigureConsumers,
    ],
    builder.Configuration);

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

app.MapDefaultEndpoints();
app.MapEndpoints();

app.Run();
