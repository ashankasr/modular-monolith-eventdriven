using ModularMonolithEventDriven.Common.Application.Abstractions;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.Products.GetProducts;

public sealed record GetProductsQuery : IQuery<List<ProductDto>>;

public sealed record ProductDto(Guid Id, string Name, string Sku, int StockQuantity, decimal Price);
