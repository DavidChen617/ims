using Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Api.Configuration;

public static class JwtConfig
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddJwtAuthentication(IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = configuration["JwtSetting:Authority"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new()
                    {
                        ValidateIssuer = true,
                        ValidIssuer = configuration["JwtSetting:Authority"],
                        ValidateAudience = true,
                        ValidAudience = configuration["JwtSetting:Audience"],
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true
                    };
                });

            return services;
        }

        public IServiceCollection AddOrderingAuthorization()
        {
            // Role 必須跟 Organization 的 Domain.Users.Role enum 對得上(Ordering 沒有那個型別的本地副本)。
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOrWarehouseAdmin",
                    policy => policy.RequireRole(nameof(Role.Admin), nameof(Role.WarehouseAdmin)));
                options.AddPolicy("AnyWarehouseRole",
                    policy => policy.RequireRole(nameof(Role.Admin), nameof(Role.WarehouseAdmin), nameof(Role.WarehouseUser)));
                options.AddPolicy("WarehouseUserOnly", policy => policy.RequireRole(nameof(Role.WarehouseUser)));
                options.AddPolicy("WarehouseAdminOnly", policy => policy.RequireRole(nameof(Role.WarehouseAdmin)));
                options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(Role.Admin)));
                options.AddPolicy("WarehouseStaffOnly",
                    policy => policy.RequireRole(nameof(Role.WarehouseAdmin), nameof(Role.WarehouseUser)));
            });

            return services;
        }
    }
}
