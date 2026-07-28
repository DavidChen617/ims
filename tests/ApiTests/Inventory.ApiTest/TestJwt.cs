using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Inventory.ApiTest;

// 對齊 Organization TokenGenerator 的 claim 形狀:ClaimTypes.NameIdentifier/Name/Role
// 都直接寫成它們的長格式 URI,"name" 維持用簡短的 JWT-registered claim 名稱(反正它也不在
// JWT handler 預設的 inbound claim map 裡,兩種寫法都不會有重新映射的歧義)。
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
