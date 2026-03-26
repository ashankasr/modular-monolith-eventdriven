using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Modules.Payments.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Payments.Domain;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Extensions;

public static class PaymentsModuleExtensions
{
    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb")));

        services.AddScoped<IPaymentsUnitOfWork>(sp => sp.GetRequiredService<PaymentsDbContext>());
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddApplication(
            typeof(Application.AssemblyReference).Assembly);

        return services;
    }
}
