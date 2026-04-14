using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;
using ModularMonolithEventDriven.Modules.Payments.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Payments.Domain;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Consumers;
using ModularMonolithEventDriven.Modules.Payments.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Payments.Infrastructure.Extensions;

public static class PaymentsModule
{
    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PaymentsDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb"));
            opts.AddInterceptors(sp.GetRequiredService<OutboxMessagesInterceptor>());
        });

        services.AddScoped<IPaymentsUnitOfWork>(sp => sp.GetRequiredService<PaymentsDbContext>());
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IOutboxMessageProcessor, OutboxMessageProcessor<PaymentsDbContext>>();

        services.AddApplication(
            typeof(Application.AssemblyReference).Assembly);

        return services;
    }

    public static void ConfigureConsumers(IRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<OrderCancelledPaymentConsumer>();
        configurator.AddConsumer<ProcessPaymentCommandConsumer>();
    }
}
