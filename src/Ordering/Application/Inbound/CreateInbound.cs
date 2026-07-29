using Davish.Result;
using Domain.InboundOrders;
using Domain.Products;

using Application.Abstracts;
namespace Application.Inbound;

public sealed record CreateInboundCommand(
    string OrderNo,
    IReadOnlyList<CreateInboundItem> Items
) : ICommand<Result<CreateInboundDto>>;

// UnitPrice 是選填的 —— 沒帶的話就用商品目前自己的定價
public sealed record CreateInboundItem(Guid ProductId, string ProductNo, int Quantity, decimal? UnitPrice);

public sealed record CreateInboundDto(Guid Id, string Status);

public sealed class CreateInboundCommandHandler(
    IInboundOrderRepository repository,
    IProductRepository productRepository,
    ICurrentUser currentUser
) : ICommandHandler<CreateInboundCommand, Result<CreateInboundDto>>
{
    public async Task<Result<CreateInboundDto>> HandleAsync(
        CreateInboundCommand request, CancellationToken cancellationToken)
    {
        var requestedIds = request.Items.Select(item => item.ProductId).ToList();
        var productsResult = await productRepository.GetByIdsAsync(requestedIds, cancellationToken);

        if (!productsResult.IsSuccess)
            return productsResult.Error;

        var products = productsResult.Value.ToDictionary(p => p.Id);
        var missingItems = request.Items.Where(item => !products.ContainsKey(item.ProductId)).ToList();

        if (missingItems.Count > 0)
        {
            var missingNos = string.Join(", ", missingItems.Select(item => item.ProductNo));
            return new Error("Product.NotFound", $"Product No not found: {missingNos}", ErrorType.NotFound);
        }

        var items = request.Items
            .Select(item => (item.ProductId, item.Quantity, item.UnitPrice ?? products[item.ProductId].Price))
            .ToList();

        return await InboundOrder
            .Create(request.OrderNo, currentUser.WarehouseId!.Value, currentUser.UserId, currentUser.Name, items)
            .ThenAsync(async order =>
            {
                await repository.AddAsync(order, cancellationToken);

                return Result.Success(new CreateInboundDto(order.Id, order.Status.ToString()));
            });
    }
}
