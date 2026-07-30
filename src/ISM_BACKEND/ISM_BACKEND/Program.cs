using System.Text;
using ISM_BACKEND.Base;
using ISM_BACKEND.Middleware;
using ISM_BACKEND.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection("DatabaseSettings"));

// Base 基礎設施
builder.Services.AddScoped<DapperRepository>();
builder.Services.AddScoped<PasswordHelper>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<InboundOrderService>();
builder.Services.AddScoped<OutboundOrderService>();
builder.Services.AddScoped<StockService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DapperRepository>();
    var password = scope.ServiceProvider.GetRequiredService<PasswordHelper>();
    var config = app.Configuration;

    var seedUsername = config["SeedAdmin:Username"] ?? "admin";
    var count = await db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountUserByUsername, new { Username = seedUsername });
    if (count == 0)
    {
        await db.ExecuteAsync(IsmQueries.InsertUser, new
        {
            WarehouseId = (long?)null,
            Name = config["SeedAdmin:Name"] ?? "Admin",
            Username = seedUsername,
            PasswordHash = password.Hash(config["SeedAdmin:Password"] ?? "1qazXSW@"),
            Role = (int)Role.Admin
        });
    }
}

app.Run();
