using ModularMonolithEventDriven.Common.Domain.Primitives;

namespace ModularMonolithEventDriven.Modules.Payments.Domain;

public sealed class Payment : AuditableGuidEntity
{
    private Payment() { }

    private Payment(Guid id, Guid orderId, string customerId, decimal amount) : base(id)
    {
        OrderId = orderId;
        CustomerId = customerId;
        Amount = amount;
        Status = PaymentStatus.Processed;
    }

    public Guid OrderId { get; private set; }
    public string CustomerId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }

    public static Payment Create(Guid id, Guid orderId, string customerId, decimal amount) =>
        new(id, orderId, customerId, amount);
}

public enum PaymentStatus { Processed, Failed, Refunded }
