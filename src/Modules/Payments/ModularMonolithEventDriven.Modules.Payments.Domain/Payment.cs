using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Payments.Domain.Errors;

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

    public static Result<Payment> Create(Guid id, Guid orderId, string customerId, decimal amount)
    {
        if (orderId == Guid.Empty)
            return Result.Failure<Payment>(PaymentErrors.InvalidOrderId);

        if (string.IsNullOrWhiteSpace(customerId))
            return Result.Failure<Payment>(PaymentErrors.InvalidCustomerId);

        if (amount <= 0)
            return Result.Failure<Payment>(PaymentErrors.InvalidAmount);

        return new Payment(id, orderId, customerId, amount);
    }

    public void Refund() => Status = PaymentStatus.Refunded;
}

public enum PaymentStatus { Processed, Failed, Refunded }
