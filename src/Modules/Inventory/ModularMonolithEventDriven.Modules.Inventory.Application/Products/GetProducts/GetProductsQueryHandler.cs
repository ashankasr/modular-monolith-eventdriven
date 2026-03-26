using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<Result<List<ProductDto>>> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.StockQuantity, p.Price)).ToList();
    }
}
