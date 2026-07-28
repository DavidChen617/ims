using Domain.RefreshToken;

namespace Organization.UnitTest;

public class RefreshTokenTests
{
    [Fact]
    public void GivenRefreshToken_WhenNotYetRevoked_ThenSucceedsAndMarksAsRevoked()
    {
        var token = RefreshToken.Create("token-value", Guid.NewGuid(), DateTime.UtcNow.AddDays(7)).Value;

        var result = token.Revoke();

        Assert.True(result.IsSuccess);
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void GivenRefreshToken_WhenAlreadyRevoked_ThenFails()
    {
        var token = RefreshToken.Create("token-value", Guid.NewGuid(), DateTime.UtcNow.AddDays(7)).Value;
        token.Revoke();

        var result = token.Revoke();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void GivenValidInputs_WhenCreated_ThenPropertiesAreSetCorrectly()
    {
        var userId = Guid.CreateVersion7();
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var result = RefreshToken.Create("token-value", userId, expiresAt);

        Assert.True(result.IsSuccess);
        var token = result.Value;
        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal("token-value", token.Token);
        Assert.Equal(userId, token.UserId);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Null(token.ReplacedByToken);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void GivenExpiryInThePast_WhenChecked_ThenIsExpiredIsTrue()
    {
        var token = RefreshToken.Create("token-value", Guid.CreateVersion7(), DateTime.UtcNow.AddDays(-1)).Value;

        Assert.True(token.IsExpired);
    }

    [Fact]
    public void GivenExpiryInTheFuture_WhenChecked_ThenIsExpiredIsFalse()
    {
        var token = RefreshToken.Create("token-value", Guid.CreateVersion7(), DateTime.UtcNow.AddDays(1)).Value;

        Assert.False(token.IsExpired);
    }

    [Fact]
    public void GivenRefreshToken_WhenReplacedForTheFirstTime_ThenSucceedsAndSetsReplacedByToken()
    {
        var token = RefreshToken.Create("token-value", Guid.CreateVersion7(), DateTime.UtcNow.AddDays(7)).Value;

        var result = token.ReplaceToken("new-token-value");

        Assert.True(result.IsSuccess);
        Assert.Equal("new-token-value", token.ReplacedByToken);
    }

    [Fact]
    public void GivenRefreshToken_WhenReplacedTwice_ThenSecondReplaceFails()
    {
        var token = RefreshToken.Create("token-value", Guid.CreateVersion7(), DateTime.UtcNow.AddDays(7)).Value;
        token.ReplaceToken("first-replacement");

        var result = token.ReplaceToken("second-replacement");

        Assert.False(result.IsSuccess);
        Assert.Equal("first-replacement", token.ReplacedByToken);
    }
}
