using Mapster;
using ModularMonolithEventDriven.Modules.Inventory.Application.Products.GetProducts;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.Products;

public static class ProductMappingConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<Product, ProductDto>.NewConfig();
    }
}
