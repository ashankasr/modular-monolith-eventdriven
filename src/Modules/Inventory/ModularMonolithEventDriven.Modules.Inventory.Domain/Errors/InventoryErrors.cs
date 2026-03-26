using ModularMonolithEventDriven.Common.Domain.Results;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain.Errors;

public static class InventoryErrors
{
    public static Error ProductNotFound(Guid id) =>
        Error.NotFound("Inventory.ProductNotFound", $"Product '{id}' not found.");

    public static Error InsufficientStock(string productName) =>
        Error.Failure("Inventory.InsufficientStock", $"Insufficient stock for '{productName}'.");
}
