using Davish.Result;
using Domain.OutboundOrders;

using Application.Abstracts;
namespace Application.Outbound;

public sealed record ConfirmOutboundCommand(
    Guid OutboundOrderId
) : ICommand<Result<ConfirmOutboundDto>>;

public sealed record ConfirmOutboundDto(Guid Id, string Status);

public sealed class ConfirmOutboundCommandHandler(
    IOutboundOrderRepository repository,
    ICurrentUser currentUser
) : ICommandHandler<ConfirmOutboundCommand, Result<ConfirmOutboundDto>>
{
    public async Task<Result<ConfirmOutboundDto>> HandleAsync(
        ConfirmOutboundCommand request, CancellationToken cancellationToken)
    {
        return await repository
            .GetByIdAsync(request.OutboundOrderId, cancellationToken)
            .ThenAsync(async order => await order
                .Confirm(currentUser.UserId, currentUser.Name)
                .ThenAsync(async () =>
                {
                    await repository.SaveAsync(order, cancellationToken);

                    return Result.Success(new ConfirmOutboundDto(order.Id, order.Status.ToString()));
                })
            );
    }
}
