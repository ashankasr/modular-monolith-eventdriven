using Mapster;
using ModularMonolithEventDriven.Modules.Orders.Application.Orders.GetOrder;
using ModularMonolithEventDriven.Modules.Orders.Domain;

namespace ModularMonolithEventDriven.Modules.Orders.Application.Orders;

public static class OrderMappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<Order, OrderResponse>
            .NewConfig()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Items, src => src.Items.Adapt<List<OrderItemResponse>>());

        TypeAdapterConfig<OrderItem, OrderItemResponse>.NewConfig();
    }
}
