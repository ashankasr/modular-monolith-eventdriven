using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Infrastructure.EventBus;
using ModularMonolithEventDriven.Common.Infrastructure.Jobs;
using ModularMonolithEventDriven.Common.Infrastructure.Options;
using ModularMonolithEventDriven.Common.Infrastructure.Outbox;
using Quartz;

namespace ModularMonolithEventDriven.Common.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<IRegistrationConfigurator>[] moduleConsumerConfigurators,
        IConfiguration configuration)
    {
        AddMessageBroker(services, moduleConsumerConfigurators, configuration);
        AddOutboxJob(services);

        services.AddSingleton<OutboxMessagesInterceptor>();
        services.AddScoped<IEventBus, MassTransitEventBus>();

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

    private static void AddOutboxJob(IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.AddJob<ProcessOutboxMessagesJob>(ProcessOutboxMessagesJob.Key, j => j.StoreDurably());

            q.AddTrigger(t => t
                .ForJob(ProcessOutboxMessagesJob.Key)
                .WithIdentity("outbox-trigger")
                .WithSimpleSchedule(s => s
                    .WithIntervalInSeconds(30)
                    .RepeatForever()));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    }
}
