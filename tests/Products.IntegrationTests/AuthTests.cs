using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Products.Api.Authentication;

namespace Products.IntegrationTests;

public class AuthTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    [Fact]
    public async Task Token_WithInvalidCredentials_Returns401()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/auth/token", new TokenRequest("admin", "wrong"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Token_WithMissingCredentials_Returns400()
    {
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/auth/token", new TokenRequest("", ""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/v1/products")]
    [InlineData("GET", "/api/v1/products?colour=red")]
    [InlineData("POST", "/api/v1/products")]
    public async Task ProductEndpoints_WithoutToken_Return401(string method, string path)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProductEndpoints_WithTokenSignedByUnknownKey_Return401()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(signingKey: new string('x', 64), expires: DateTime.UtcNow.AddMinutes(5)));

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProductEndpoints_WithExpiredToken_Return401()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", CreateToken(ProductsApiFactory.SigningKey, expires: DateTime.UtcNow.AddMinutes(-5)));

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithReadOnlyScope_Returns403()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("viewer");

        using var response = await client.PostAsJsonAsync("/api/v1/products", new { name = "Chair", colour = "Red", price = 1m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static string CreateToken(string signingKey, DateTime expires) => new JsonWebTokenHandler().CreateToken(
        new SecurityTokenDescriptor
        {
            Issuer = ProductsApiFactory.Issuer,
            Audience = ProductsApiFactory.Audience,
            Subject = new ClaimsIdentity([new Claim(ProductScopes.ClaimType, ProductScopes.Read)]),
            NotBefore = expires.AddMinutes(-30),
            IssuedAt = expires.AddMinutes(-30),
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        });
}
