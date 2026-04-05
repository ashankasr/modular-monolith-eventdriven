using Microsoft.AspNetCore.Builder;
using ModularMonolithEventDriven.Modules.Inventory.Presentation;
using ModularMonolithEventDriven.Modules.Notifications.Presentation;
using ModularMonolithEventDriven.Modules.Orders.Presentation;
using ModularMonolithEventDriven.Modules.Payments.Presentation;

namespace ModularMonolithEventDriven.Api.Extensions;

internal static class WebApplicationExtensions
{
    internal static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapOrdersEndpoints();
        app.MapInventoryEndpoints();
        app.MapPaymentsEndpoints();
        app.MapNotificationsEndpoints();

        return app;
    }
}
