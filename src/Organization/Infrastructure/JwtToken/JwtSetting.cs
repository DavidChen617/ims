namespace Infrastructure.JwtToken;

public sealed class JwtSetting
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int RefreshTokenExpiresInDay { get; set; }
    public int AccessTokenExpiresInMinuets { get; set; }
    
    public DateTime AccessTokenExpiredAt => DateTime.UtcNow.AddMinutes(AccessTokenExpiresInMinuets);
    public DateTime RefreshTokenExpiredAt => DateTime.UtcNow.AddDays(RefreshTokenExpiresInDay);
}
