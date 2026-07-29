using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Password;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hashedPassword, string providedPassword);
}

public class PasswordHasher(IPasswordHasher<object> hasher) : IPasswordHasher
{
    private readonly object _dummy = new();

    public string Hash(string password) => hasher.HashPassword(_dummy, password);
    
    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = hasher.VerifyHashedPassword(_dummy, hashedPassword, providedPassword);

        return result == PasswordVerificationResult.Success;
    }
}
