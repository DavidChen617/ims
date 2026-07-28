using Davish.Result;

namespace Domain.RefreshToken;

public interface IRefreshTokenRepository
{
    Task<Result> AddAsync(RefreshToken refreshToken, CancellationToken ct);
    Task<Result<RefreshToken>> GetByTokenAsync(string refreshToken, CancellationToken ct);
    Task<Result> SaveAsync(RefreshToken refreshToken, CancellationToken ct);
}
