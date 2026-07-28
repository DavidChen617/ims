using Api.Extension;
using Davish.Result;
using Domain.RefreshToken;
using Domain.Users;
using Domain.Warehouse;
using Infrastructure.JwtToken;
using Infrastructure.Password;
using Microsoft.Extensions.Options;

namespace Api.Endpoints.v1.Auth;

public static class LoginEndpoint
{
    extension(RouteGroupBuilder authV1Group)
    {
        public RouteGroupBuilder MapLoginEndpoint()
        {
            authV1Group.MapPost("login", Handle)
                .Produces<LoginDto>()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .WithName("Login")
                .WithSummary("User login")
                .WithDescription("Verify the account password and return AccessToken and RefreshToken after success.");

            return authV1Group;
        }
    }

    private static async Task<IResult> Handle(
        LoginRequest request,
        IPasswordHasher hasher,
        ITokenGenerator tokenGenerator,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<JwtSetting> jwtOptions,
        CancellationToken ct)
    {
        var result = await userRepository.GetByUsername(request.Username, ct)
            .Then(user => hasher.Verify(user.PasswordHash, request.Password)
                ? Result.Success(user)
                : new Error("User.Password", "Password error", ErrorType.Unauthorized))
            .ThenAsync(async user =>
            {
                var claims = await TokenClaims.FromUserAsync(user, warehouseRepository, ct);
                var accessToken = tokenGenerator.GenerateToken(claims);
                var refreshToken = tokenGenerator.GenerateRefreshToken();

                var refresh = RefreshToken.Create(
                    refreshToken,
                    user.Id,
                    jwtOptions.Value.RefreshTokenExpiredAt
                ).Value;

                await refreshTokenRepository.AddAsync(refresh, ct);

                return Result.Success(new LoginDto(user.Id, accessToken, refreshToken, refresh.ExpiresAt));
            });

        return result.ToOk();
    }
}

public record LoginRequest(string Username, string Password);

public record LoginDto(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiredAt
);
