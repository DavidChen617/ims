using Api.Endpoints.v1;
using Api.Endpoints.WellKnown;
using Api.ExceptionHandling;
using Domain.Users;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var otlpEndpoint = builder.Configuration["Otel:OtlpEndpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "organization"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(o =>
            {
                // http.route 樣板本身不代入是故意的(避免高基數),但版本號是個例外——
                // 版本數量有限、不會無限成長,代入後不會有基數問題,單純是路由樣板
                // 沒有具體化這個值,所以要自己從 RequestedApiVersion 補回去。
                // 用 EnrichWithHttpResponse(對應 request 結束)而不是 EnrichWithHttpRequest
                // (對應 request 開始,UseRouting 都還沒跑)—— 版本號要等 endpoint 執行完
                // 才解析得出來,太早讀 RequestedApiVersion 只會拿到 null。
                o.EnrichWithHttpResponse = (activity, response) =>
                {
                    if (response.HttpContext.RequestedApiVersion is { } version)
                        activity.DisplayName = activity.DisplayName.Replace(
                            "v{version:apiVersion}", version.ToString("'v'VVV"));
                };
            })
            .AddHttpClientInstrumentation()
            .AddSource("Npgsql");

        if (otlpEndpoint is not null)
            tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));
    });

var rsaKey = BuildRsaKey();
rsaKey.KeyId = "org-rsa-1"; // 讓 JWT 的 kid header 跟 JWKS 裡發布的 kid 對得上。

builder.Services.AddSingleton(rsaKey);

builder.Services.AddApiVersioning(o =>
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
        document.Info.Title = "Organization API";
        return Task.CompletedTask;
    });
});

builder.Services.AddProblemDetails(o =>
{
    o.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

        Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});

builder.Services.AddValidation();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql");

builder.Services
    .AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(Role.Admin)));
        options.AddPolicy("AdminOrWarehouseAdminOnly",
            policy => policy.RequireRole(nameof(Role.Admin), nameof(Role.WarehouseAdmin)));
        options.AddPolicy("WarehouseStaffOnly",
            policy => policy.RequireRole(nameof(Role.WarehouseAdmin), nameof(Role.WarehouseUser)));
        options.AddPolicy("WarehouseAdminOnly", policy => policy.RequireRole(nameof(Role.WarehouseAdmin)));
        options.AddPolicy("UserOnly", policy => policy.RequireRole(nameof(Role.WarehouseUser)));
    })
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSetting:Issuer"],
            ValidAudience = builder.Configuration["JwtSetting:Audience"],
            IssuerSigningKey = rsaKey
        };
    });

builder.Services.AddInfrastructure(builder.Configuration);

// 依序輪詢:先給 NpgsqlExceptionHandler 機會接住漏檢查就直接打到 DB 的例外,
// 其他所有沒被特化處理的例外最後都掉到 GlobalExceptionHandler。
builder.Services
    .AddExceptionHandler<NpgsqlExceptionHandler>()
    .AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.MapOpenApi().WithDocumentPerVersion();

app.MapWellKnownEndpoints();

app.UseAuthentication();
app.UseAuthorization();

var api = app.NewVersionedApi().MapGroup("/api/v{version:apiVersion}");
api.MapV1Endpoints();

app.MapHealthChecks("/healthz", new HealthCheckOptions { ResponseWriter = HealthCheckFormatHelper.WriteResponse });

app.Run();

RsaSecurityKey BuildRsaKey()
{
    var rsa = RSA.Create();
    rsa.ImportFromPem(File.ReadAllText(builder.Configuration["RSA_PEM_PATH"]!));
    return new RsaSecurityKey(rsa);
}
