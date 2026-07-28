using Dapper;
using Davish.Result;
using Domain.RefreshToken;

namespace Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(IOrganizationUnitOfWork unitOfWork) : IRefreshTokenRepository
{
    public async Task<Result> AddAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            insert into refresh_token(id, token, user_id, created_at, expires_at)
            values(@Id,  @Token, @UserId, @CreatedAt, @ExpiresAt);
            """,
            refreshToken,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );
        
        await unitOfWork.Connection.ExecuteAsync(cmd);
        
        return Result.Success();
    }

    public async Task<Result<RefreshToken>> GetByTokenAsync(string refreshToken, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            select id, token, replaced_by_token, user_id, created_at, expires_at, revoke_at
            from refresh_token
            where token = @RefreshToken
            """,
            new { RefreshToken = refreshToken },
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        var token = await unitOfWork.Connection.QuerySingleOrDefaultAsync<RefreshToken>(cmd);

        return token;
    }

    public async Task<Result> SaveAsync(RefreshToken refreshToken, CancellationToken ct)
    {
        var cmd = new CommandDefinition(
            """
            update refresh_token
            set replaced_by_token = @ReplacedByToken, revoke_at = @RevokeAt
            where id = @Id
            """,
            refreshToken,
            cancellationToken: ct,
            transaction: unitOfWork.Transaction
        );

        await unitOfWork.Connection.ExecuteAsync(cmd);

        return Result.Success();
    }
}
