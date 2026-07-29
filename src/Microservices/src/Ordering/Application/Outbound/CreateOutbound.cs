using Davish.Result;
using Domain.OutboundOrders;
using Domain.Products;

using Application.Abstracts;
namespace Application.Outbound;

public sealed record CreateOutboundCommand(
    string OrderNo,
    IReadOnlyList<CreateOutboundItem> Items
) : ICommand<Result<CreateOutboundDto>>;

public sealed record CreateOutboundItem(Guid ProductId, string ProductNo, int Quantity);

public sealed record CreateOutboundDto(Guid Id, string Status);

public sealed class CreateOutboundCommandHandler(
    IOutboundOrderRepository repository,
    IProductRepository productRepository,
    ICurrentUser currentUser
) : ICommandHandler<CreateOutboundCommand, Result<CreateOutboundDto>>
{
    public async Task<Result<CreateOutboundDto>> HandleAsync(
        CreateOutboundCommand request, CancellationToken cancellationToken)
    {
        var requestedIds = request.Items.Select(item => item.ProductId).ToList();
        var existingIdsResult = await productRepository.GetExistingIdsAsync(requestedIds, cancellationToken);

        if (!existingIdsResult.IsSuccess)
            return existingIdsResult.Error;

        var existingIds = existingIdsResult.Value.ToHashSet();
        var missingItems = request.Items.Where(item => !existingIds.Contains(item.ProductId)).ToList();

        if (missingItems.Count > 0)
        {
            var missingNos = string.Join(", ", missingItems.Select(item => item.ProductNo));
            return new Error("Product.NotFound", $"Product No not found: {missingNos}", ErrorType.NotFound);
        }

        var items = request.Items
            .Select(item => (item.ProductId, item.Quantity))
            .ToList();

        return await OutboundOrder
            .Create(request.OrderNo, currentUser.WarehouseId!.Value, currentUser.UserId, currentUser.Name, items)
            .ThenAsync(async order =>
            {
                await repository.AddAsync(order, cancellationToken);

                return Result.Success(new CreateOutboundDto(order.Id, order.Status.ToString()));
            });
    }
}
