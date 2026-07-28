using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Ordering.ApiTest;

public static class TestJwt
{
    public static string Create(
        SymmetricSecurityKey signingKey,
        string role,
        Guid? warehouseId,
        Guid? userId = null,
        string username = "test-user",
        string name = "Test User")
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, (userId ?? Guid.CreateVersion7()).ToString()),
            new(JwtRegisteredClaimNames.Name, name),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role)
        ];

        if (warehouseId is not null)
            claims.Add(new Claim("warehouseId", warehouseId.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: CustomWebApplicationFactory.Issuer,
            audience: CustomWebApplicationFactory.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
