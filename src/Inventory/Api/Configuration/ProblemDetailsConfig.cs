using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Api.Configuration;

public static class ProblemDetailsConfig
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProblemDetailsWithCustomizeDetail()
        {
            services.AddProblemDetails(o =>
            {
                o.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                    context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

                    Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                    context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
                };
            });

            return services;
        }
    }
}
