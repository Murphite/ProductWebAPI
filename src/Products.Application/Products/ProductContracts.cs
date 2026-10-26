using Products.Domain.Products;

namespace Products.Application.Products;

public sealed record CreateProductRequest(string Name, string? Description, string Colour, decimal Price);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string Colour,
    decimal Price,
    DateTimeOffset CreatedAt)
{
    public static ProductResponse FromDomain(Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Colour,
        product.Price,
        product.CreatedAt);
}
