using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModularMonolithEventDriven.Common.Application.Extensions;
using ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Persistence;

namespace ModularMonolithEventDriven.Modules.Notifications.Infrastructure.Extensions;

public static class NotificationsModuleExtensions
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("ModularMonolithEventDrivenDb")));

        services.AddApplication(
            typeof(Application.AssemblyReference).Assembly);

        return services;
    }
}
