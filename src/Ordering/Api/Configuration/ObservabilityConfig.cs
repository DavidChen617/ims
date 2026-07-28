using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SharedKernel.Telemetry;

namespace Api.Configuration;

public static class ObservabilityConfig
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddObservability(IConfiguration configuration)
        {
            var otlpEndpoint = configuration["Otel:OtlpEndpoint"];

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName: "ordering"))
                .WithTracing(tracing =>
                {
                    tracing
                        .AddAspNetCoreInstrumentation(o =>
                        {
                            o.EnrichWithHttpResponse = (activity, response) =>
                            {
                                if (response.HttpContext.RequestedApiVersion is { } version)
                                    activity.DisplayName = activity.DisplayName.Replace(
                                        "v{version:apiVersion}", version.ToString("'v'VVV"));
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .AddSource("Npgsql")
                        .AddSource(MessagingActivitySource.Name);

                    if (otlpEndpoint is not null)
                        tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
                });

            return services;
        }
    }
}
