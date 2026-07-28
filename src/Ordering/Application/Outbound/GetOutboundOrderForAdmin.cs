using Davish.Result;

namespace Application.Outbound;

public sealed record GetOutboundOrderForAdminQuery(Guid Id) : IQuery<Result<OutboundOrderDto>>;

public sealed class GetOutboundOrderForAdminQueryHandler(
    IOutboundOrderReader reader
) : IQueryHandler<GetOutboundOrderForAdminQuery, Result<OutboundOrderDto>>
{
    public async Task<Result<OutboundOrderDto>> HandleAsync(
        GetOutboundOrderForAdminQuery request, CancellationToken cancellationToken)
    {
        return await reader.GetByIdAsync(request.Id, cancellationToken);
    }
}
