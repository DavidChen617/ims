using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Api.Extension;
using Davish.Result;
using Domain.RefreshToken;
using Domain.Users;
using Domain.Warehouse;
using Infrastructure.JwtToken;
using Infrastructure.Password;
using Microsoft.Extensions.Options;

namespace Api.Endpoints.v1.Auth;

public static class egisterWarehouseUserEndpoint
{
    extension(RouteGroupBuilder authV1Group)
    {
        public RouteGroupBuilder MapRegisterWarehouseUserEndpoint()
        {
            authV1Group.MapPost("warehouseAdmin/register/warehouseUser", Handle)
                .Produces<RegisterWarehouseUserDto>()
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("RegisterWarehouseUserByWarehouseAdmin")
                .WithSummary("Register a new user")
                .WithDescription(
                    "Create a new user account with a unique username and password. Returns 400 if the username already exists.")
                .RequireAuthorization("WarehouseAdminOnly");

            return authV1Group;
        }
    }

    private static async Task<IResult> Handle(
        RegisterFromWarehouseAdminRequest request,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenGenerator tokenGenerator,
        IPasswordHasher hasher,
        IOptions<JwtSetting> jwtOptions,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var warehouseId = Guid.Parse(user.FindFirst("warehouseId")!.Value);

        var existing = await userRepository.GetByUsername(request.Username, ct);

        var result = await (existing.IsSuccess
                ? Result.Failure<User>(new Error("Auth.Register", "Username already exists", ErrorType.BadRequest))
                : User.Register(warehouseId, request.Name, request.Username, hasher.Hash(request.Password),
                    Role.WarehouseUser))
            .ThenAsync(async newUser =>
            {
                var claims = await TokenClaims.FromUserAsync(newUser, warehouseRepository, ct);
                var accessToken = tokenGenerator.GenerateToken(claims);
                var refreshToken = RefreshToken.Create(tokenGenerator.GenerateRefreshToken(), newUser.Id,
                    jwtOptions.Value.RefreshTokenExpiredAt).Value;

                await userRepository.AddAsync(newUser, ct);
                await refreshTokenRepository.AddAsync(refreshToken, ct);

                return Result.Success(new RegisterWarehouseUserDto(newUser.Id, accessToken, refreshToken.Token,
                    refreshToken.ExpiresAt));
            });

        return result.ToOk();
    }
}

public record RegisterFromWarehouseAdminRequest(
    string Name,
    string Username,
    [property: MinLength(8)] string Password);

public record RegisterWarehouseUserDto(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiredAt
);
