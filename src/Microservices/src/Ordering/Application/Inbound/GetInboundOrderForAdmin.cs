using Davish.Result;

namespace Application.Inbound;

public sealed record GetInboundOrderForAdminQuery(Guid Id) : IQuery<Result<InboundOrderDto>>;

public sealed class GetInboundOrderForAdminQueryHandler(
    IInboundOrderReader reader
) : IQueryHandler<GetInboundOrderForAdminQuery, Result<InboundOrderDto>>
{
    public async Task<Result<InboundOrderDto>> HandleAsync(
        GetInboundOrderForAdminQuery request, CancellationToken cancellationToken)
    {
        return await reader.GetByIdAsync(request.Id, cancellationToken);
    }
}
