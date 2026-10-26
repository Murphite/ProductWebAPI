using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Products.Api.Authentication;

namespace Products.UnitTests.Api;

public class TokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly TokenService _sut = new(
        Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = new string('k', 64),
            TokenLifetime = TimeSpan.FromMinutes(30),
        }),
        Options.Create(new AuthOptions
        {
            Users =
            [
                new DemoUser { Username = "admin", Password = "secret", Scopes = [ProductScopes.Read, ProductScopes.Write] },
            ],
        }),
        new FakeTimeProvider(Now));

    [Fact]
    public void IssueToken_WithValidCredentials_ReturnsSignedTokenWithScopes()
    {
        var response = _sut.IssueToken("ADMIN", "secret");

        Assert.NotNull(response);
        Assert.Equal("Bearer", response.TokenType);
        Assert.Equal(1800, response.ExpiresIn);

        var token = new JsonWebToken(response.AccessToken);
        Assert.Equal("test-issuer", token.Issuer);
        Assert.Contains("test-audience", token.Audiences);
        Assert.Equal("admin", token.Subject);
        Assert.Equal(Now.AddMinutes(30).UtcDateTime, token.ValidTo);
        var scopes = token.Claims.Where(c => c.Type == ProductScopes.ClaimType).Select(c => c.Value);
        Assert.Equal([ProductScopes.Read, ProductScopes.Write], scopes);
    }

    [Theory]
    [InlineData("admin", "wrong")]
    [InlineData("admin", "")]
    [InlineData("nobody", "secret")]
    [InlineData("nobody", "")]
    public void IssueToken_WithInvalidCredentials_ReturnsNull(string username, string password)
    {
        Assert.Null(_sut.IssueToken(username, password));
    }
}
