using Products.Domain.Products;

namespace Products.Application.Abstractions;

public interface IProductRepository
{
    void Add(Product product);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lists products, optionally restricted to a normalised colour.</summary>
    Task<IReadOnlyList<Product>> ListAsync(string? normalizedColour, CancellationToken cancellationToken);
}
