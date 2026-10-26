using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Products.Api.Authentication;

public interface ITokenService
{
    /// <summary>Returns a signed access token, or <c>null</c> when the credentials are invalid.</summary>
    TokenResponse? IssueToken(string username, string password);
}

public sealed record TokenRequest(string Username, string Password);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn, IReadOnlyList<string> Scopes);

internal sealed class TokenService(
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider) : ITokenService
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public TokenResponse? IssueToken(string username, string password)
    {
        var user = authOptions.Value.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));

        // Always perform the comparison so response timing does not reveal whether the user exists.
        var passwordMatches = FixedTimeEquals(user?.Password ?? string.Empty, password);
        if (user is null || !passwordMatches)
        {
            return null;
        }

        var options = jwtOptions.Value;
        var now = timeProvider.GetUtcNow();
        var expires = now.Add(options.TokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
        };
        claims.AddRange(user.Scopes.Select(scope => new Claim(ProductScopes.ClaimType, scope)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = credentials,
        };

        return new TokenResponse(
            TokenHandler.CreateToken(descriptor),
            "Bearer",
            (int)options.TokenLifetime.TotalSeconds,
            user.Scopes);
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash) && expected.Length > 0;
    }
}
