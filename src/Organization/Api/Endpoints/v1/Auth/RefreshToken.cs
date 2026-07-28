using Api.Extension;
using Davish.Result;
using Domain.RefreshToken;
using Domain.Users;
using Domain.Warehouse;
using Infrastructure.JwtToken;
using Microsoft.Extensions.Options;

namespace Api.Endpoints.v1.Auth;

public static class RefreshTokenEndpoint
{
    extension(RouteGroupBuilder authV1Group)
    {
        public RouteGroupBuilder MapRefreshTokenEndpoint()
        {
            authV1Group.MapPost("refresh/token", Handle)
                .Produces<RefreshTokenDto>()
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .WithName("RefreshToken")
                .WithSummary("refresh token")
                .WithDescription(
                    "Exchange a valid, non-expired, non-revoked Refresh Token for a new AccessToken and RefreshToken pair. The old Refresh Token is revoked and replaced. Returns 401 if the Refresh Token is invalid, expired, or already revoked.");

            return authV1Group;
        }
    }

    private static async Task<IResult> Handle(
        RefreshTokenRequest request,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        IWarehouseRepository warehouseRepository,
        ITokenGenerator tokenGenerator,
        IOptions<JwtSetting> jwtOptions,
        CancellationToken ct)
    {
        var result = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct)
            .Then(refresh =>
                refresh.IsExpired ? new Error("Auth.RefreshToken", "Refresh token is expired", ErrorType.Unauthorized) :
                refresh.IsRevoked ? new Error("Auth.RefreshToken", "Refresh token is revoked", ErrorType.Unauthorized) :
                Result.Success(refresh)
            ).ThenAsync(async refreshToken =>
                {
                    var userResult = await userRepository.GetByIdAsync(refreshToken.UserId, ct);
                    return userResult.Then(user => (refreshToken, user));
                }
            ).ThenAsync(async pair =>
            {
                (RefreshToken refreshToken, User user) = pair;

                var revoke = refreshToken.Revoke();
                if (!revoke.IsSuccess)
                    return Result.Failure<RefreshTokenDto>(revoke.Error);

                var newToken = tokenGenerator.GenerateRefreshToken();
                var newRefresh = RefreshToken.Create(newToken, user.Id, jwtOptions.Value.RefreshTokenExpiredAt).Value;

                var replace = refreshToken.ReplaceToken(newToken);
                if (!replace.IsSuccess)
                    return Result.Failure<RefreshTokenDto>(replace.Error);

                var save = await refreshTokenRepository.SaveAsync(refreshToken, ct);
                if (!save.IsSuccess)
                    return Result.Failure<RefreshTokenDto>(save.Error);

                await refreshTokenRepository.AddAsync(newRefresh, ct);

                var claims = await TokenClaims.FromUserAsync(user, warehouseRepository, ct);

                return Result.Success(new RefreshTokenDto(tokenGenerator.GenerateToken(claims), newRefresh.Token,
                    newRefresh.ExpiresAt));
            });

        return result.ToOk();
    }
}

public record RefreshTokenRequest(Guid UserId, string RefreshToken);

public record RefreshTokenDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiredAt);
