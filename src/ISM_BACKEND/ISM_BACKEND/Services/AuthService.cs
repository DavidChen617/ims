using ISM_BACKEND.Base;
using ISM_BACKEND.Models;
using Microsoft.Extensions.Configuration;

namespace ISM_BACKEND.Services;

public class AuthService
{
    private readonly DapperRepository _db;
    private readonly IJwtTokenService _jwt;
    private readonly PasswordHelper _password;
    private readonly int _refreshTokenExpiresInDays;

    public AuthService(DapperRepository db, IJwtTokenService jwt, PasswordHelper password, IConfiguration configuration)
    {
        _db = db;
        _jwt = jwt;
        _password = password;
        _refreshTokenExpiresInDays = int.Parse(configuration["JwtSettings:RefreshTokenExpiresInDays"] ?? "7");
    }

    public async Task<TokenResponse?> LoginAsync(string username, string password)
    {
        var user = await _db.QueryFirstOrDefaultAsync<UserRow>(IsmQueries.FindUserByUsername, new { Username = username });
        if (user == null || !_password.Verify(user.PasswordHash, password))
            return null;

        return await IssueTokenAsync(user);
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var token = await _db.QueryFirstOrDefaultAsync<RefreshTokenRow>(IsmQueries.FindRefreshTokenByToken, new { Token = refreshToken });
        if (token == null || token.RevokeTime != null || token.ExpireTime < DateTime.UtcNow)
            return false;

        await _db.ExecuteAsync(IsmQueries.RevokeRefreshToken, new { RefreshTokenId = token.RefreshTokenId });
        return true;
    }

    public async Task<TokenResponse?> RefreshAsync(long userId, string refreshToken)
    {
        var token = await _db.QueryFirstOrDefaultAsync<RefreshTokenRow>(IsmQueries.FindRefreshTokenByToken, new { Token = refreshToken });
        if (token == null || token.UserId != userId || token.RevokeTime != null || token.ExpireTime < DateTime.UtcNow)
            return null;

        var user = await _db.QueryFirstOrDefaultAsync<UserRow>(IsmQueries.FindUserById, new { UserId = userId });
        if (user == null)
            return null;

        _db.BeginTransaction();
        try
        {
            var newToken = _jwt.GenerateRefreshToken();
            await _db.ExecuteAsync(IsmQueries.ReplaceRefreshToken, new { RefreshTokenId = token.RefreshTokenId, NewToken = newToken });
            var expireAt = DateTime.UtcNow.AddDays(_refreshTokenExpiresInDays);
            await _db.ExecuteAsync(IsmQueries.InsertRefreshToken, new { Token = newToken, UserId = userId, ExpireTime = expireAt });
            _db.Commit();

            var accessToken = _jwt.GenerateToken(user.UserId, user.Username, user.Name, ((Role)user.Role).ToString(), user.WarehouseId);
            return new TokenResponse
            {
                userId = user.UserId,
                accessToken = accessToken,
                refreshToken = newToken,
                refreshTokenExpiredAt = expireAt
            };
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    // Admin 建立任意角色使用者(含 WarehouseAdmin)
    public async Task<TokenResponse> RegisterUserAsync(long? warehouseId, string name, string username, string password, string role)
    {
        var count = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountUserByUsername, new { Username = username });
        if (count > 0)
            throw new ArgumentException($"帳號 {username} 已存在");

        if (!Enum.TryParse<Role>(role, out var parsedRole))
            throw new ArgumentException($"角色 {role} 不合法");

        var userId = await InsertUserTx(warehouseId, name, username, password, parsedRole);
        var user = await _db.QueryFirstOrDefaultAsync<UserRow>(IsmQueries.FindUserById, new { UserId = userId });
        return await IssueTokenAsync(user!);
    }

    // WarehouseAdmin 建立自己倉庫的 WarehouseUser
    public async Task<TokenResponse> RegisterWarehouseUserAsync(long warehouseId, string name, string username, string password)
    {
        var count = await _db.QueryFirstOrDefaultAsync<int>(IsmQueries.CountUserByUsername, new { Username = username });
        if (count > 0)
            throw new ArgumentException($"帳號 {username} 已存在");

        var userId = await InsertUserTx(warehouseId, name, username, password, Role.WarehouseUser);
        var user = await _db.QueryFirstOrDefaultAsync<UserRow>(IsmQueries.FindUserById, new { UserId = userId });
        return await IssueTokenAsync(user!);
    }

    private async Task<long> InsertUserTx(long? warehouseId, string name, string username, string password, Role role)
    {
        _db.BeginTransaction();
        try
        {
            var userId = await _db.ExecuteInsertWithIdentityAsync(IsmQueries.InsertUser, new
            {
                WarehouseId = warehouseId,
                Name = name,
                Username = username,
                PasswordHash = _password.Hash(password),
                Role = (int)role
            });
            _db.Commit();
            return userId;
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    private async Task<TokenResponse> IssueTokenAsync(UserRow user)
    {
        _db.BeginTransaction();
        try
        {
            var refreshToken = _jwt.GenerateRefreshToken();
            var expireAt = DateTime.UtcNow.AddDays(_refreshTokenExpiresInDays);
            await _db.ExecuteAsync(IsmQueries.InsertRefreshToken, new { Token = refreshToken, UserId = user.UserId, ExpireTime = expireAt });
            _db.Commit();

            var accessToken = _jwt.GenerateToken(user.UserId, user.Username, user.Name, ((Role)user.Role).ToString(), user.WarehouseId);
            return new TokenResponse
            {
                userId = user.UserId,
                accessToken = accessToken,
                refreshToken = refreshToken,
                refreshTokenExpiredAt = expireAt
            };
        }
        catch
        {
            _db.Rollback();
            throw;
        }
    }

    private sealed class UserRow
    {
        public long UserId { get; set; }
        public long? WarehouseId { get; set; }
        public string Name { get; set; } = "";
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public int Role { get; set; }
        public DateTime CreateTime { get; set; }
    }

    private sealed class RefreshTokenRow
    {
        public long RefreshTokenId { get; set; }
        public string Token { get; set; } = "";
        public string? ReplacedByToken { get; set; }
        public long UserId { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime ExpireTime { get; set; }
        public DateTime? RevokeTime { get; set; }
    }
}
