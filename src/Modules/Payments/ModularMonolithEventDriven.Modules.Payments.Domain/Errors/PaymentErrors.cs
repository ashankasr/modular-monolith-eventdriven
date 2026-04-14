using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Modules.Payments.Domain.Errors;

public static class PaymentErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Payment.NotFound", $"Payment for order '{id}' was not found.");

    public static readonly Error InvalidOrderId =
        Error.Validation("Payment.InvalidOrderId", "Order ID cannot be empty.");

    public static readonly Error InvalidCustomerId =
        Error.Validation("Payment.InvalidCustomerId", "Customer ID cannot be empty.");

    public static readonly Error InvalidAmount =
        Error.Validation("Payment.InvalidAmount", "Payment amount must be greater than zero.");
}
