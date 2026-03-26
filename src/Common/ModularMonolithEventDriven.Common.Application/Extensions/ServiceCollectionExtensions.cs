using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Behaviors;
using System.Reflection;

namespace ModularMonolithEventDriven.Common.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(assemblies);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssemblies(assemblies);

        return services;
    }
}
