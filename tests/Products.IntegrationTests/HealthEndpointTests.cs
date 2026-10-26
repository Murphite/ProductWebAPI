using System.Net;
using System.Text.Json;

namespace Products.IntegrationTests;

public class HealthEndpointTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    public async Task HealthEndpoints_AreAnonymousAndReportHealthy(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_IncludesDatabaseCheck()
    {
        using var client = factory.CreateClient();

        var json = await client.GetStringAsync("/health");

        using var body = JsonDocument.Parse(json);
        var check = Assert.Single(body.RootElement.GetProperty("checks").EnumerateArray());
        Assert.Equal("database", check.GetProperty("name").GetString());
    }
}
