using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Infrastructure.Options;

namespace ModularMonolithEventDriven.Common.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<IRegistrationConfigurator>[] moduleConsumerConfigurators,
        IConfiguration configuration)
    {
        AddMessageBroker(services, moduleConsumerConfigurators, configuration);

        return services;
    }

    private static void AddMessageBroker(
        IServiceCollection services,
        Action<IRegistrationConfigurator>[] moduleConsumerConfigurators,
        IConfiguration configuration)
    {
        services.AddMassTransit(configure =>
        {
            foreach (Action<IRegistrationConfigurator> configureConsumers in moduleConsumerConfigurators)
            {
                configureConsumers(configure);
            }

            configure.SetKebabCaseEndpointNameFormatter();

            ConfigureRabbitMq(configure, configuration);
        });
    }

    private static void ConfigureRabbitMq(
        IBusRegistrationConfigurator configure,
        IConfiguration configuration)
    {
        var rabbitConnectionString = configuration.GetConnectionString(RabbitMqOptions.ConnectionName)
            ?? throw new InvalidOperationException(
                $"Connection string '{RabbitMqOptions.ConnectionName}' not found.");

        configure.UsingRabbitMq((context, configurator) =>
        {
            configurator.Host(new Uri(rabbitConnectionString));

            configurator.UseMessageRetry(r => r.Exponential(
                5,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMinutes(5),
                TimeSpan.FromSeconds(5)));

            configurator.ConfigureEndpoints(context);
        });
    }
}
