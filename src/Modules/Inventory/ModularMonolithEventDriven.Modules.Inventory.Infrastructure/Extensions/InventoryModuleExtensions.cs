using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Modules.Inventory.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Inventory.Domain;
using ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Inventory.Infrastructure.Extensions;

public static class InventoryModuleExtensions
{
    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        
        services.AddDbContext<InventoryDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb")));

        services.AddScoped<IInventoryUnitOfWork>(sp => sp.GetRequiredService<InventoryDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockReservationRepository, StockReservationRepository>();

        services.AddApplication(
            typeof(Application.AssemblyReference).Assembly);

        return services;
    }
}
