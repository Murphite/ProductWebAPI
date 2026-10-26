using Microsoft.EntityFrameworkCore;
using Products.Application.Abstractions;
using Products.Domain.Products;

namespace Products.Infrastructure.Persistence;

internal sealed class ProductRepository(ProductsDbContext dbContext) : IProductRepository
{
    public void Add(Product product) => dbContext.Products.Add(product);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> ListAsync(string? normalizedColour, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();

        if (normalizedColour is not null)
        {
            query = query.Where(p => p.NormalizedColour == normalizedColour);
        }

        return await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken);
    }
}
