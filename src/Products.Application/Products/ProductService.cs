using FluentValidation;
using Microsoft.Extensions.Logging;
using Products.Application.Abstractions;
using Products.Domain.Products;

namespace Products.Application.Products;

public sealed partial class ProductService(
    IProductRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<CreateProductRequest> validator,
    TimeProvider timeProvider,
    ILogger<ProductService> logger) : IProductService
{
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var product = Product.Create(
            request.Name,
            request.Description,
            request.Colour,
            request.Price,
            timeProvider.GetUtcNow());

        repository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        LogProductCreated(logger, product.Id, product.Name);

        return ProductResponse.FromDomain(product);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(id, cancellationToken);
        return product is null ? null : ProductResponse.FromDomain(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> ListAsync(string? colour, CancellationToken cancellationToken)
    {
        var normalizedColour = string.IsNullOrWhiteSpace(colour) ? null : Product.NormalizeColour(colour);
        var products = await repository.ListAsync(normalizedColour, cancellationToken);
        return products.Select(ProductResponse.FromDomain).ToList();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Created product {ProductId} ({ProductName})")]
    private static partial void LogProductCreated(ILogger logger, Guid productId, string productName);
}
