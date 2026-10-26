using Products.Application.Products;

namespace Products.UnitTests.Application;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_Passes()
    {
        var result = _validator.Validate(new CreateProductRequest("Chair", "Oak chair", "Light Blue", 99.95m));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Red", 1, nameof(CreateProductRequest.Name))]
    [InlineData("Chair", "", 1, nameof(CreateProductRequest.Colour))]
    [InlineData("Chair", "R3d", 1, nameof(CreateProductRequest.Colour))]
    [InlineData("Chair", "<script>", 1, nameof(CreateProductRequest.Colour))]
    [InlineData("Chair", "Red", -1, nameof(CreateProductRequest.Price))]
    [InlineData("Chair", "Red", 1.999, nameof(CreateProductRequest.Price))]
    [InlineData("Chair", "Red", 1000000.01, nameof(CreateProductRequest.Price))]
    public void Validate_WithInvalidField_ReportsThatField(string name, string colour, decimal price, string expectedProperty)
    {
        var result = _validator.Validate(new CreateProductRequest(name, null, colour, price));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == expectedProperty);
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_Fails()
    {
        var request = new CreateProductRequest("Chair", new string('x', 2001), "Red", 1m);

        var result = _validator.Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProductRequest.Description));
    }
}
