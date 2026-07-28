using Davish.Result;
using Domain.InboundOrders;

using Application.Abstracts;
namespace Application.Inbound;

public sealed record RejectInboundCommand(
    Guid InboundOrderId,
    string Reason
) : ICommand<Result<RejectInboundDto>>;

public sealed record RejectInboundDto(Guid Id, string Status);

public sealed class RejectInboundCommandHandler(
    IInboundOrderRepository repository,
    ICurrentUser currentUser
) : ICommandHandler<RejectInboundCommand, Result<RejectInboundDto>>
{
    public async Task<Result<RejectInboundDto>> HandleAsync(
        RejectInboundCommand request, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.InboundOrderId, cancellationToken)
            .ThenAsync(async order => await order
                .Reject(currentUser.UserId, currentUser.Name, request.Reason)
                .ThenAsync(async () =>
                {
                    await repository.SaveAsync(order, cancellationToken);

                    return Result.Success(new RejectInboundDto(order.Id, order.Status.ToString()));
                })
            );
    }
}
