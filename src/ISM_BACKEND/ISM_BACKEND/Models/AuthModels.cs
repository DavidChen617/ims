namespace ISM_BACKEND.Models;

public class LoginRequest
{
    public string username { get; set; } = "";
    public string password { get; set; } = "";
}

public class TokenResponse
{
    public long userId { get; set; }
    public string accessToken { get; set; } = "";
    public string refreshToken { get; set; } = "";
    public DateTime refreshTokenExpiredAt { get; set; }
}

public class LogoutRequest
{
    public string refreshToken { get; set; } = "";
}

public class RefreshRequest
{
    public long userId { get; set; }
    public string refreshToken { get; set; } = "";
}

public class RegisterUserRequest
{
    public long? warehouseId { get; set; }
    public string name { get; set; } = "";
    public string username { get; set; } = "";
    public string password { get; set; } = "";
    public string role { get; set; } = ""; // Admin / WarehouseAdmin / WarehouseUser
}

public class RegisterWarehouseUserRequest
{
    public string name { get; set; } = "";
    public string username { get; set; } = "";
    public string password { get; set; } = "";
}
