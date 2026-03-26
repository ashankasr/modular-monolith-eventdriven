using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Modules.Orders.Domain.Errors;

public static class OrderErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Order.NotFound", $"Order with id '{id}' was not found.");

    public static readonly Error EmptyItems =
        Error.Validation("Order.EmptyItems", "Order must have at least one item.");
}
