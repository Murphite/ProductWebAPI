using Products.Domain.Common;
using Products.Domain.Products;

namespace Products.UnitTests.Domain;

public class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidValues_TrimsInputAndNormalisesColour()
    {
        var product = Product.Create("  Chair  ", "  Comfy  ", "  Dark Blue ", 49.99m, Now);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Chair", product.Name);
        Assert.Equal("Comfy", product.Description);
        Assert.Equal("Dark Blue", product.Colour);
        Assert.Equal("DARK BLUE", product.NormalizedColour);
        Assert.Equal(49.99m, product.Price);
        Assert.Equal(Now, product.CreatedAt);
    }

    [Fact]
    public void Create_WithWhitespaceDescription_StoresNull()
    {
        var product = Product.Create("Chair", "   ", "Red", 1m, Now);

        Assert.Null(product.Description);
    }

    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        var product = Product.Create("Chair", null, "Red", 10m, Now);

        var domainEvent = Assert.IsType<ProductCreatedEvent>(Assert.Single(product.DomainEvents));
        Assert.Equal(product.Id, domainEvent.ProductId);
        Assert.Equal("Chair", domainEvent.Name);
        Assert.Equal("Red", domainEvent.Colour);
        Assert.Equal(10m, domainEvent.Price);
        Assert.Equal(Now, domainEvent.OccurredAt);
    }

    [Fact]
    public void ClearDomainEvents_RemovesRaisedEvents()
    {
        var product = Product.Create("Chair", null, "Red", 10m, Now);

        product.ClearDomainEvents();

        Assert.Empty(product.DomainEvents);
    }

    [Theory]
    [InlineData("", "Red", 1)]
    [InlineData("   ", "Red", 1)]
    [InlineData("Chair", "", 1)]
    [InlineData("Chair", "Red", -0.01)]
    public void Create_WithInvalidValues_ThrowsDomainException(string name, string colour, decimal price)
    {
        Assert.Throws<DomainException>(() => Product.Create(name, null, colour, price, Now));
    }

    [Fact]
    public void Create_WithNameTooLong_ThrowsDomainException()
    {
        var name = new string('a', Product.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => Product.Create(name, null, "Red", 1m, Now));
    }

    [Theory]
    [InlineData("red", "RED")]
    [InlineData(" Red ", "RED")]
    [InlineData("rEd", "RED")]
    public void NormalizeColour_IsCaseAndWhitespaceInsensitive(string input, string expected)
    {
        Assert.Equal(expected, Product.NormalizeColour(input));
    }
}
