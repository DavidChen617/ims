using Api.Identity;
using Application;
using Application.Stocks;
using Davish.Result;

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
                .AddRequestHandler<ListStocksQuery, Result<PagedResult<StockDto>>, ListStocksQueryHandler>();

            return services;
        }
    }
}
