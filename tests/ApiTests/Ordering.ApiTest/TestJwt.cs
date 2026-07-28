using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Ordering.ApiTest;

// 完全對齊 Organization TokenGenerator 的 claim 形狀:ClaimTypes.NameIdentifier、
// ClaimTypes.Name、ClaimTypes.Role 全部直接寫成長格式 URI,所以這裡不依賴 JWT handler
// 的 inbound claim map。只有 "name" 維持用簡短的 JWT-registered claim 名稱
// (它也不在那個 map 裡,所以兩種寫法都不會有歧義)。
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
