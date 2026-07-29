using Api.Middleware;
using Application;
using Application.BehaviorDecorator;
using Application.Inbound;
using Application.Outbound;
using Application.Products;
using Davish.Result;
using CurrentUser = Api.Identity.CurrentUser;

using Application.Abstracts;
namespace Api.Configuration;

public static class ApplicationConfig
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            services
                .AddScoped<ICurrentUser, CurrentUser>()
                .AddSendr();

            services
                .AddRequestHandler<CreateInboundCommand, Result<CreateInboundDto>, CreateInboundCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<GetInboundOrderQuery, Result<InboundOrderDto>, GetInboundOrderQueryHandler>()
                .AddRequestHandler<GetInboundOrderForAdminQuery, Result<InboundOrderDto>,
                    GetInboundOrderForAdminQueryHandler>()
                .AddRequestHandler<ListPendingInboundOrdersQuery, Result<PagedResult<PendingInboundOrderDto>>,
                    ListPendingInboundOrdersQueryHandler>()
                .AddRequestHandler<ListInboundHistoryQuery, Result<InboundHistoryResultDto>,
                    ListInboundHistoryQueryHandler>(x => x.Decorator.With<CachingDecorator>())
                .AddRequestHandler<ListInboundOrderHistoryQuery, Result<InboundOrderHistoryResultDto>,
                    ListInboundOrderHistoryQueryHandler>(x => x.Decorator.With<CachingDecorator>())
                .AddRequestHandler<ConfirmInboundCommand, Result<ConfirmInboundDto>, ConfirmInboundCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<RejectInboundCommand, Result<RejectInboundDto>, RejectInboundCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<CreateOutboundCommand, Result<CreateOutboundDto>, CreateOutboundCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<GetOutboundOrderQuery, Result<OutboundOrderDto>, GetOutboundOrderQueryHandler>()
                .AddRequestHandler<GetOutboundOrderForAdminQuery, Result<OutboundOrderDto>,
                    GetOutboundOrderForAdminQueryHandler>()
                .AddRequestHandler<ListPendingOutboundOrdersQuery, Result<PagedResult<PendingOutboundOrderDto>>,
                    ListPendingOutboundOrdersQueryHandler>()
                .AddRequestHandler<ListOutboundHistoryQuery, Result<PagedResult<OutboundHistoryDto>>,
                    ListOutboundHistoryQueryHandler>(x => x.Decorator.With<CachingDecorator>())
                .AddRequestHandler<ListPendingOutboundQuantitiesQuery, Result<PendingOutboundQuantitiesDto>,
                    ListPendingOutboundQuantitiesQueryHandler>()
                .AddRequestHandler<ConfirmOutboundCommand, Result<ConfirmOutboundDto>,
                    ConfirmOutboundCommandHandler>(x => x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<RejectOutboundCommand, Result<RejectOutboundDto>, RejectOutboundCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<CreateProductCommand, Result<CreateProductDto>, CreateProductCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<GetProductQuery, Result<ProductDto>, GetProductQueryHandler>()
                .AddRequestHandler<ListProductsQuery, Result<PagedResult<ProductDto>>, ListProductsQueryHandler>()
                .AddRequestHandler<CreateProductUnitCommand, Result, CreateProductUnitCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<DeleteProductUnitCommand, Result, DeleteProductUnitCommandHandler>(x =>
                    x.Decorator.With<TransactionalDecorator>())
                .AddRequestHandler<ListProductUnitsQuery, Result<ProductUnitsDto>,
                    ListProductUnitsQueryHandler>();

            return services;
        }
    }
}
