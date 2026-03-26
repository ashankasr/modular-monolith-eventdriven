using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ModularMonolithEventDriven.Modules.Notifications.Presentation;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        // Notifications are event-driven. In a real app, add GET /api/notifications for history.
        return app;
    }
}
