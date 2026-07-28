using Api.Extension;
using Davish.Result;
using Domain.RefreshToken;

namespace Api.Endpoints.v1.Auth;

public static class LogoutEndpoint
{
    extension(RouteGroupBuilder authV1Group)
    {
        public RouteGroupBuilder MapLogoutEndpoint()
        {
            authV1Group.MapPost("logout", Handle)
                .Produces(StatusCodes.Status204NoContent)
                .Produces(StatusCodes.Status401Unauthorized)
                .WithName("Logout")
                .WithSummary("User logout")
                .WithDescription(
                    "Revoke the specified Refresh Token so that it can no longer be used to exchange for new Access Token. If the Token has expired, 400 will be returned.")
                .RequireAuthorization();

            return authV1Group;
        }
    }

    private static async Task<IResult> Handle(
        LogoutRequest request,
        IRefreshTokenRepository refreshTokenRepository,
        CancellationToken ct)
    {
        var result = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct)
            .Then(token =>
                token.IsExpired ? new Error("Auth.Logout", "Refresh token is expired", ErrorType.Unauthorized) :
                token.IsRevoked ? new Error("Auth.Logout", "Refresh token is already revoked", ErrorType.Unauthorized) :
                Result.Success(token))
            .ThenAsync(async token =>
            {
                var revoke = token.Revoke();
                if (!revoke.IsSuccess)
                    return Result.Failure(revoke.Error);

                var save = await refreshTokenRepository.SaveAsync(token, ct);
                if (!save.IsSuccess)
                    return Result.Failure(save.Error);

                return Result.Success();
            });

        return result.ToNoContent();
    }
}

public record LogoutRequest(string RefreshToken);
