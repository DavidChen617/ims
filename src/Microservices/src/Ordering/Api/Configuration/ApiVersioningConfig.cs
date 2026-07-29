using Asp.Versioning;

namespace Api.Configuration;

public static class ApiVersioningConfig
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiVersioningWithOpenApi()
        {
            services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(1);
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ReportApiVersions = true;
            }).AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            }).AddOpenApi(options =>
            {
                options.Document.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info.Title = "Ordering API";
                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}
