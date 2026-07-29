using Davish.Result;
using SharedKernel;

namespace Domain.RefreshToken;

public sealed class RefreshToken : Entity<Guid>
{
    public string Token { get; private set; } = null!;
    public string? ReplacedByToken { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokeAt { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsRevoked => RevokeAt is not null;

    public static Result<RefreshToken> Create(string token, Guid userId, DateTime expiresAt)
    {
        var refreshToken = new RefreshToken()
        {
            Id = Guid.CreateVersion7(),
            Token = token,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };

        return refreshToken;
    }

    public Result Revoke()
    {
        if (RevokeAt is not null)
            return new Error("RefreshToken.Revoke", "refresh already revoked");

        RevokeAt = DateTime.UtcNow;

        return Result.Success();
    }

    public Result ReplaceToken(string newToken)
    {
        if (ReplacedByToken is not null)
            return new Error("RefreshToken.ReplaceToken", "ReplacedByToken already set");

        ReplacedByToken = newToken;

        return Result.Success(this);
    }
}
