using Davish.Result;
using Domain.OutboundOrders;

using Application.Abstracts;
namespace Application.Outbound;

public sealed record RejectOutboundCommand(
    Guid OutboundOrderId,
    string Reason
) : ICommand<Result<RejectOutboundDto>>;

public sealed record RejectOutboundDto(Guid Id, string Status);

public sealed class RejectOutboundCommandHandler(
    IOutboundOrderRepository repository,
    ICurrentUser currentUser
) : ICommandHandler<RejectOutboundCommand, Result<RejectOutboundDto>>
{
    public async Task<Result<RejectOutboundDto>> HandleAsync(
        RejectOutboundCommand request, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(request.OutboundOrderId, cancellationToken)
            .ThenAsync(async order =>
            {
                return await order.Reject(currentUser.UserId, currentUser.Name, request.Reason)
                    .ThenAsync(async () =>
                    {
                        await repository.SaveAsync(order, cancellationToken);

                        return Result.Success(new RejectOutboundDto(order.Id, order.Status.ToString()));
                    });
            });
    }
}
