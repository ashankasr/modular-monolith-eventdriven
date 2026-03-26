using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string Sku,
    int StockQuantity,
    decimal Price) : ICommand<Guid>;
