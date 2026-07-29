using Domain.Users;
using Infrastructure.Password;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Persistence;

public sealed class DataSeeder(IServiceScopeFactory factory): IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = factory.CreateScope();
        var provider = scope.ServiceProvider;

        var userRepository = provider.GetRequiredService<IUserRepository>();
        var existing = await userRepository.GetByUsername("admin", cancellationToken);
        
        if (!existing.IsSuccess)
        {
            var hasher = provider.GetRequiredService<IPasswordHasher>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            var passwordHash = hasher.Hash(configuration["Admin:Password"]!);
            var admin = User.RegisterAdmin("Admin", "admin",passwordHash, Role.Admin).Value;
            
            await userRepository.AddAsync(admin, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
