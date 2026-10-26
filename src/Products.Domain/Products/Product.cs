using Products.Domain.Common;

namespace Products.Domain.Products;

public sealed class Product : Entity
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int ColourMaxLength = 50;

    // Required by EF Core.
    private Product()
    {
        Name = string.Empty;
        Colour = string.Empty;
        NormalizedColour = string.Empty;
    }

    private Product(Guid id, string name, string? description, string colour, decimal price, DateTimeOffset createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        Colour = colour;
        NormalizedColour = NormalizeColour(colour);
        Price = price;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    /// <summary>Colour as supplied by the caller (display value).</summary>
    public string Colour { get; private set; }

    /// <summary>Upper-cased colour used for case-insensitive, index-friendly lookups.</summary>
    public string NormalizedColour { get; private set; }

    public decimal Price { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Product Create(string name, string? description, string colour, decimal price, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required.");
        }

        if (string.IsNullOrWhiteSpace(colour))
        {
            throw new DomainException("Product colour is required.");
        }

        if (price < 0)
        {
            throw new DomainException("Product price cannot be negative.");
        }

        name = name.Trim();
        colour = colour.Trim();
        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        if (name.Length > NameMaxLength)
        {
            throw new DomainException($"Product name cannot exceed {NameMaxLength} characters.");
        }

        if (colour.Length > ColourMaxLength)
        {
            throw new DomainException($"Product colour cannot exceed {ColourMaxLength} characters.");
        }

        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Product description cannot exceed {DescriptionMaxLength} characters.");
        }

        var product = new Product(Guid.CreateVersion7(createdAt), name, description, colour, price, createdAt);
        product.Raise(new ProductCreatedEvent(
            Guid.CreateVersion7(createdAt), createdAt, product.Id, product.Name, product.Colour, product.Price));

        return product;
    }

    public static string NormalizeColour(string colour) => colour.Trim().ToUpperInvariant();
}
