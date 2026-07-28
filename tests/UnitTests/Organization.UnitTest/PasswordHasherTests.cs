using Infrastructure.Password;
using Microsoft.AspNetCore.Identity;

namespace Organization.UnitTest;

public class PasswordHasherTests
{
    private static IPasswordHasher CreateHasher() => new PasswordHasher(new PasswordHasher<object>());

    [Fact]
    public void GivenPassword_WhenHashedThenVerifiedWithSamePassword_ThenSucceeds()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("correct-password");

        var result = hasher.Verify(hash, "correct-password");

        Assert.True(result);
    }

    [Fact]
    public void GivenPassword_WhenVerifiedWithWrongPassword_ThenFails()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("correct-password");

        var result = hasher.Verify(hash, "wrong-password");

        Assert.False(result);
    }

    [Fact]
    public void GivenPassword_WhenHashed_ThenHashDoesNotContainThePlainTextPassword()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("correct-password");

        Assert.DoesNotContain("correct-password", hash);
    }
}
