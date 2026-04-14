using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Orders.Domain.Events;

namespace ModularMonolithEventDriven.Modules.Orders.Domain;

public sealed class Order : AuditableGuidEntity
{
    private readonly List<OrderItem> _items = [];

    private Order() { }

    private Order(
        Guid id,
        string customerId,
        string customerEmail,
        List<OrderItem> items) : base(id)
    {
        CustomerId = customerId;
        CustomerEmail = customerEmail;
        _items = items;
        Status = OrderStatus.Pending;
        TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity);
    }

    public string CustomerId { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    public static Result<Order> Create(Guid id, string customerId, string customerEmail, List<OrderItem> items)
    {
        if (string.IsNullOrEmpty(customerId))
            return Result.Failure<Order>(Error.Validation("Order.InvalidCustomerId", "Customer ID cannot be empty."));

        if (string.IsNullOrEmpty(customerEmail))
            return Result.Failure<Order>(Error.Validation("Order.InvalidCustomerEmail", "Customer email cannot be empty."));

        if (items.Count == 0)
            return Result.Failure<Order>(Error.Validation("Order.NoItems", "Order must have at least one item."));

        var order = new Order(id, customerId, customerEmail, items);
        order.RaiseDomainEvent(new OrderCreatedDomainEvent(id, customerId));
        return order;
    }

    public void MarkAsStockReserved() => Status = OrderStatus.StockReserved;
    public void MarkAsPaymentProcessed() => Status = OrderStatus.PaymentProcessed;
    public void MarkAsCompleted() => Status = OrderStatus.Completed;
    public void MarkAsFailed(string reason)
    {
        Status = OrderStatus.Failed;
        FailureReason = reason;
    }
    public void MarkAsCancelled(string reason)
    {
        Status = OrderStatus.Cancelled;
        RaiseDomainEvent(new OrderCancelledDomainEvent(Id, reason));
    }
}
