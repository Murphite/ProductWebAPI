using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Products.Domain.Products;

namespace Products.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(p => p.Colour).HasMaxLength(Product.ColourMaxLength).IsRequired();
        builder.Property(p => p.NormalizedColour).HasMaxLength(Product.ColourMaxLength).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => p.NormalizedColour);

        builder.Ignore(p => p.DomainEvents);
    }
}
