using Microsoft.AspNetCore.Identity;

namespace ISM_BACKEND.Base;

public class PasswordHelper
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public bool Verify(string passwordHash, string providedPassword)
        => _hasher.VerifyHashedPassword(new object(), passwordHash, providedPassword) != PasswordVerificationResult.Failed;
}
