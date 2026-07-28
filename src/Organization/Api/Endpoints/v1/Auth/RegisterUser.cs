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

public static class RegisterUserEndpoint
{
    extension(RouteGroupBuilder authV1Group)
    {
        public RouteGroupBuilder MapRegisterUserEndpoint()
        {
            authV1Group.MapPost("admin/register/user", Handle)
                .Produces<RegisterUserDto>()
                .Produces(StatusCodes.Status400BadRequest)
                .WithName("RegisterUserByAdmin")
                .WithSummary("Register a new user")
                .WithDescription(
                    "Create a new user account with a unique username and password. Returns 400 if the username already exists.")
                .RequireAuthorization("AdminOnly");

            return authV1Group;
        }
    }

    private static async Task<IResult> Handle(
        RegisterUserFromAdminRequest request,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenGenerator tokenGenerator,
        IPasswordHasher hasher,
        IOptions<JwtSetting> jwtOptions,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var existing = await userRepository.GetByUsername(request.Username, ct);

        var result = await (existing.IsSuccess
                ? Result.Failure<User>(new Error("Auth.Register", "Username already exists", ErrorType.BadRequest))
                : User.Register(request.WarehouseId, request.Name, request.Username, hasher.Hash(request.Password),
                    request.Role))
            .ThenAsync(async newUser =>
            {
                var claims = await TokenClaims.FromUserAsync(newUser, warehouseRepository, ct);
                var accessToken = tokenGenerator.GenerateToken(claims);

                var refreshToken = RefreshToken.Create(tokenGenerator.GenerateRefreshToken(), newUser.Id,
                    jwtOptions.Value.RefreshTokenExpiredAt).Value;

                await userRepository.AddAsync(newUser, ct);
                await refreshTokenRepository.AddAsync(refreshToken, ct);

                return Result.Success(new RegisterUserDto(newUser.Id, accessToken, refreshToken.Token,
                    refreshToken.ExpiresAt));
            });

        return result.ToOk();
    }
}

public record RegisterUserFromAdminRequest(
    Guid WarehouseId,
    string Name,
    string Username,
    [property: MinLength(8)] string Password,
    [property: EnumDataType(typeof(Role))] Role Role);

public record RegisterUserDto(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiredAt
);
