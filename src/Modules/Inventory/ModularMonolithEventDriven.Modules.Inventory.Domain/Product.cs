using ModularMonolithEventDriven.Common.Domain.Primitives;

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

    public static Product Create(Guid id, string name, string sku, int stockQuantity, decimal price) =>
        new(id, name, sku, stockQuantity, price);

    public bool HasSufficientStock(int quantity) => StockQuantity >= quantity;

    public void ReserveStock(int quantity)
    {
        if (!HasSufficientStock(quantity))
            throw new InvalidOperationException($"Insufficient stock for product {Name}.");
        StockQuantity -= quantity;
    }

    public void ReleaseStock(int quantity) => StockQuantity += quantity;
}
