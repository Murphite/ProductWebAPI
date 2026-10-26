using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Products;
using Products.Domain.Products;
using Products.Infrastructure.Persistence;

namespace Products.IntegrationTests;

public class ProductsEndpointTests(ProductsApiFactory factory) : IClassFixture<ProductsApiFactory>
{
    [Fact]
    public async Task UsesIsolatedTestDatabase()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();

        Assert.Equal(factory.ConnectionString, dbContext.Database.GetConnectionString());
    }

    [Fact]
    public async Task CreateProduct_WithValidRequest_Returns201WithLocationAndBody()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("admin");
        var request = new CreateProductRequest("Desk", "Standing desk", "Walnut", 349.50m);

        using var response = await client.PostAsJsonAsync("/api/v1/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProductResponse>();
        Assert.NotNull(created);
        Assert.Equal("Desk", created.Name);
        Assert.Equal("Walnut", created.Colour);
        Assert.Equal(349.50m, created.Price);
        Assert.Equal($"/api/v1/products/{created.Id}", response.Headers.Location?.AbsolutePath);

        var fetched = await client.GetFromJsonAsync<ProductResponse>(response.Headers.Location);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task CreateProduct_WritesProductCreatedEventToOutbox()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("admin");

        var created = await CreateProductAsync(client, "Outbox Lamp", "Amber");

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();
        var messages = await dbContext.OutboxMessages.AsNoTracking().ToListAsync();
        Assert.Contains(messages, m =>
            m.Type == nameof(ProductCreatedEvent) &&
            m.Payload.Contains(created.Id.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateProduct_WithInvalidRequest_Returns400ValidationProblem()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("admin");

        using var response = await client.PostAsJsonAsync("/api/v1/products", new { name = "", colour = "R3d", price = -5 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Colour", out _));
        Assert.True(errors.TryGetProperty("Price", out _));
        Assert.True(body.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task CreateProduct_WithMalformedJson_Returns400()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("admin");
        using var content = new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/products", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListProducts_ReturnsAllProductsAsJson()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("admin");
        var first = await CreateProductAsync(client, "List Item A", UniqueColour());
        var second = await CreateProductAsync(client, "List Item B", UniqueColour());

        using var response = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();
        Assert.NotNull(products);
        Assert.Contains(products, p => p.Id == first.Id);
        Assert.Contains(products, p => p.Id == second.Id);
    }

    [Fact]
    public async Task ListProducts_FilteredByColour_ReturnsOnlyMatchingProducts_CaseInsensitively()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("admin");
        var colour = UniqueColour();
        var red1 = await CreateProductAsync(client, "Filter A", colour);
        var red2 = await CreateProductAsync(client, "Filter B", colour.ToUpperInvariant());
        await CreateProductAsync(client, "Filter C", UniqueColour());

        var products = await client.GetFromJsonAsync<List<ProductResponse>>(
            $"/api/v1/products?colour={Uri.EscapeDataString(" " + colour.ToLowerInvariant() + " ")}");

        Assert.NotNull(products);
        Assert.Equal([red1.Id, red2.Id], products.Select(p => p.Id));
    }

    [Fact]
    public async Task ListProducts_FilteredByUnknownColour_ReturnsEmptyArray()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("viewer");

        var products = await client.GetFromJsonAsync<List<ProductResponse>>($"/api/v1/products?colour={UniqueColour()}");

        Assert.NotNull(products);
        Assert.Empty(products);
    }

    [Fact]
    public async Task GetProduct_WhenMissing_Returns404()
    {
        using var client = await factory.CreateAuthenticatedClientAsync("viewer");

        using var response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<ProductResponse> CreateProductAsync(HttpClient client, string name, string colour)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/products", new CreateProductRequest(name, null, colour, 10m));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    // Tests share one database per class, so each uses its own colour to stay independent.
    private static string UniqueColour() =>
        "c" + new string(Guid.NewGuid().ToString("N").Take(12).Select(ch => (char)('a' + (ch % 26))).ToArray());
}
