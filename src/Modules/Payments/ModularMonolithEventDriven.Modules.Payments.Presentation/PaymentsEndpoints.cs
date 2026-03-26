using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace ModularMonolithEventDriven.Modules.Payments.Presentation;

public static class PaymentsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        // Payments are internal — triggered via events, no direct HTTP endpoints needed for the demo
        // Could add GET /api/payments/{orderId} for status checking in a real app
        return app;
    }
}
