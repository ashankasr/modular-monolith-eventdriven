using ModularMonolithEventDriven.Common.Domain.Primitives;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Inventory.Domain.Errors;

namespace ModularMonolithEventDriven.Modules.Inventory.Domain;

public sealed class Product : AuditableGuidEntity
{
    private Product() { }

    private Product(Guid id, string name, string sku, int stockQuantity, decimal price) : base(id)
    {
        Name = name;
        Sku = sku;
        StockQuantity = stockQuantity;
        Price = price;
    }

    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public int StockQuantity { get; private set; }
    public decimal Price { get; private set; }

    public static Result<Product> Create(Guid id, string name, string sku, int stockQuantity, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(Error.Validation("Product.InvalidName", "Product name cannot be empty."));

        if (string.IsNullOrWhiteSpace(sku))
            return Result.Failure<Product>(Error.Validation("Product.InvalidSku", "Product SKU cannot be empty."));

        if (stockQuantity < 0)
            return Result.Failure<Product>(Error.Validation("Product.InvalidStockQuantity", "Stock quantity cannot be negative."));

        if (price <= 0)
            return Result.Failure<Product>(Error.Validation("Product.InvalidPrice", "Product price must be greater than zero."));

        return new Product(id, name, sku, stockQuantity, price);
    }

    public bool HasSufficientStock(int quantity) => StockQuantity >= quantity;

    public Result ReserveStock(int quantity)
    {
        if (!HasSufficientStock(quantity))
            return Result.Failure(InventoryErrors.InsufficientStock(Name));

        StockQuantity -= quantity;
        return Result.Success();
    }

    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}
