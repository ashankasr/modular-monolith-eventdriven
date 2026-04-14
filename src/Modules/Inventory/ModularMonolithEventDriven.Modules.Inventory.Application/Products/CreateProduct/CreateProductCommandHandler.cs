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
        var result = Product.Create(Guid.NewGuid(), command.Name, command.Sku, command.StockQuantity, command.Price);
        if (result.IsFailure)
            return Result.Failure<Guid>(result.Error);

        productRepository.Add(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.Id;
    }
}
