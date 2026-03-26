using ModularMonolithEventDriven.Common.Application.Abstractions;
using ModularMonolithEventDriven.Common.Domain.Results;
using ModularMonolithEventDriven.Modules.Inventory.Application.Abstractions;
using ModularMonolithEventDriven.Modules.Inventory.Domain;

namespace ModularMonolithEventDriven.Modules.Inventory.Application.Products.CreateProduct;

public sealed class CreateProductCommandHandler(
    IProductRepository productRepository,
    IInventoryUnitOfWork unitOfWork) : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = Product.Create(Guid.NewGuid(), command.Name, command.Sku, command.StockQuantity, command.Price);
        productRepository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
