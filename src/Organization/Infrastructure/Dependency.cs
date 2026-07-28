using Dapper;
using Domain.RefreshToken;
using Domain.Users;
using Domain.Warehouse;
using Infrastructure.JwtToken;
using Infrastructure.Password;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel;

namespace Infrastructure;

public static class Dependency
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure(IConfiguration configuration)
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;

            services
                .Configure<JwtSetting>(configuration.GetSection(nameof(JwtSetting)))
                .AddSingleton<IPasswordHasher<object>, PasswordHasher<object>>()
                .AddSingleton<IPasswordHasher, PasswordHasher>()
                .AddSingleton<ITokenGenerator, JwtTokenGenerator>()
                .AddScoped<IOrganizationUnitOfWork, OrganizationUnitOfWork>()
                .AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IOrganizationUnitOfWork>())
                .AddScoped<IRefreshTokenRepository, RefreshTokenRepository>()
                .AddScoped<IUserRepository, UserRepository>()
                .AddScoped<IWarehouseRepository, WarehouseRepository>()
                .AddHostedService<DataSeeder>()
                .AddNpgsqlDataSource(configuration.GetConnectionString("DefaultConnection")!);
            
            return services;
        }
    }
}
