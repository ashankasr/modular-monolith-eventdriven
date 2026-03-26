using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Modules.Orders.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Orders.Domain;
using ModularMonolithEventDriven.Modules.Orders.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Orders.Infrastructure.Extensions;

public static class OrdersModuleExtensions
{
    public static IServiceCollection AddOrdersModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb")));

        services.AddScoped<IOrdersUnitOfWork>(sp => sp.GetRequiredService<OrdersDbContext>());
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.AddApplication(
            typeof(Application.AssemblyReference).Assembly);

        return services;
    }
}
