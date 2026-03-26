using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Orders.Domain;

public sealed class OrderItem : Entity<Guid>
{
    private OrderItem() { }

    public OrderItem(Guid id, Guid orderId, Guid productId, string productName, int quantity, decimal unitPrice)
        : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
}
