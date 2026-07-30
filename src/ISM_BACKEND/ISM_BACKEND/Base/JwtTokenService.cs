using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ISM_BACKEND.Base;

public interface IJwtTokenService
{
    string GenerateToken(long userId, string username, string name, string role, long? warehouseId);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpiresInMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["JwtSettings:SecretKey"] ?? "";
        _issuer = configuration["JwtSettings:Issuer"] ?? "";
        _audience = configuration["JwtSettings:Audience"] ?? "";
        _accessTokenExpiresInMinutes = int.Parse(configuration["JwtSettings:AccessTokenExpiresInMinutes"] ?? "30");

        if (Encoding.UTF8.GetBytes(_secretKey).Length < 32)
            throw new InvalidOperationException("JwtSettings:SecretKey 長度不足 32 bytes，無法用於 HS256 簽章");
    }

    public string GenerateToken(long userId, string username, string name, string role, long? warehouseId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Name, name),
            new(ClaimTypes.Role, role),
            new("warehouseId", warehouseId?.ToString() ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ValidateLifetime = false
        };

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
