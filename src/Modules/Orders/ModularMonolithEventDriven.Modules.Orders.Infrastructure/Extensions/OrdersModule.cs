using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders;
using ModularMonolithEventDriven.Modules.Orders.Application.Saga;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Extensions;

public static class OrdersModule
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb"));
            opts.AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>());
        });

        services.AddScoped<IOrdersUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<OrdersDbContext>>();

        services.AddApplication(
            typeof(Application.AssemblyReference).Assembly);

        OrderMappingConfig.Configure();

        return services;
    }

    public static void ConfigureConsumers(IRegistrationConfigurator configurator)
    {
        configurator.AddSagaStateMachine<OrderSaga, OrderSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.ExistingDbContext<OrdersDbContext>();
            });
    }
}
