using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Products.Api.Authentication;

namespace Products.IntegrationTests;

/// <summary>
/// Boots the real API in-memory against an isolated, throw-away SQLite database file.
/// </summary>
public sealed class ProductsApiFactory : WebApplicationFactory<Program>
{
    public const string SigningKey = "integration-tests-signing-key-0123456789abcdefghij";
    public const string Issuer = "products-api-tests";
    public const string Audience = "products-api-tests";
    public const string Password = "Test-Passw0rd!";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"products-tests-{Guid.NewGuid():N}.db");
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    public string ConnectionString => $"Data Source={_databasePath}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ProductsDb", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Outbox:Enabled", "false");
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("Jwt:SigningKey", SigningKey);
        builder.UseSetting("Auth:Users:0:Username", "admin");
        builder.UseSetting("Auth:Users:0:Password", Password);
        builder.UseSetting("Auth:Users:0:Scopes:0", ProductScopes.Read);
        builder.UseSetting("Auth:Users:0:Scopes:1", ProductScopes.Write);
        builder.UseSetting("Auth:Users:1:Username", "viewer");
        builder.UseSetting("Auth:Users:1:Password", Password);
        builder.UseSetting("Auth:Users:1:Scopes:0", ProductScopes.Read);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string username)
    {
        var client = CreateClient();
        var token = await GetTokenAsync(username);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // Tokens are cached so tests stay well within the auth endpoint's rate limit.
    private async Task<string> GetTokenAsync(string username)
    {
        if (_tokens.TryGetValue(username, out var cached))
        {
            return cached;
        }

        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/token", new TokenRequest(username, Password));
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return _tokens.GetOrAdd(username, token!.AccessToken);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
