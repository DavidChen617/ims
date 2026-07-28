using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.JwtToken;

public interface ITokenGenerator
{
    string GenerateToken(TokenClaims claims);
    string GenerateRefreshToken();
}

public class JwtTokenGenerator(
    IOptions<JwtSetting> jwtOptions,
    RsaSecurityKey key) : ITokenGenerator
{
    public string GenerateToken(TokenClaims tokenClaims)
    {
        var claims = new Claim[]
        {
            new(ClaimTypes.NameIdentifier, tokenClaims.UserId.ToString()),
            new(JwtRegisteredClaimNames.Name, tokenClaims.Name),
            new(ClaimTypes.Name, tokenClaims.Username),
            new("warehouseId", tokenClaims.WarehouseId?.ToString() ?? string.Empty),
            new("warehouseName", tokenClaims.WarehouseName ?? string.Empty),
            new(ClaimTypes.Role, tokenClaims.Role.ToString()),
        };

        var cred = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: jwtOptions.Value.Issuer, 
            audience: jwtOptions.Value.Audience,
            claims: claims,
            expires: jwtOptions.Value.AccessTokenExpiredAt,
            signingCredentials: cred);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var random = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(random);
    }
}
