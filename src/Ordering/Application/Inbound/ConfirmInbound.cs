using Davish.Result;
using Domain.InboundOrders;

namespace Application.Inbound;

public sealed record ConfirmInboundCommand(
    Guid InboundOrderId
) : ICommand<Result<ConfirmInboundDto>>;

public sealed record ConfirmInboundDto(Guid Id, string Status);

public sealed class ConfirmInboundCommandHandler(
    IInboundOrderRepository repository,
    ICurrentUser currentUser
) : ICommandHandler<ConfirmInboundCommand, Result<ConfirmInboundDto>>
{
    public async Task<Result<ConfirmInboundDto>> HandleAsync(
        ConfirmInboundCommand request, CancellationToken cancellationToken)
    {
        return await repository
            .GetByIdAsync(request.InboundOrderId, cancellationToken)
            .ThenAsync(async order => await order
                .Confirm(currentUser.UserId, currentUser.Name)
                .ThenAsync(async () =>
                {
                    await repository.SaveAsync(order, cancellationToken);

                    return Result.Success(new ConfirmInboundDto(order.Id, order.Status.ToString()));
                })
            );
    }
}
