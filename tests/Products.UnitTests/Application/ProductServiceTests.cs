using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Products.Application.Abstractions;
using Products.Application.Products;
using Products.Domain.Products;

namespace Products.UnitTests.Application;

public class ProductServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IProductRepository _repository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(
            _repository,
            _unitOfWork,
            new CreateProductRequestValidator(),
            new FakeTimeProvider(Now),
            NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsProductAndCommits()
    {
        var request = new CreateProductRequest("Lamp", "Desk lamp", "Black", 25m);

        var response = await _sut.CreateAsync(request, CancellationToken.None);

        Assert.Equal("Lamp", response.Name);
        Assert.Equal("Black", response.Colour);
        Assert.Equal(Now, response.CreatedAt);
        _repository.Received(1).Add(Arg.Is<Product>(p => p.Id == response.Id && p.NormalizedColour == "BLACK"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotPersist()
    {
        var request = new CreateProductRequest("", null, "Black", 25m);

        await Assert.ThrowsAsync<ValidationException>(() => _sut.CreateAsync(request, CancellationToken.None));

        _repository.DidNotReceiveWithAnyArgs().Add(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Theory]
    [InlineData(" red ", "RED")]
    [InlineData("Red", "RED")]
    public async Task ListAsync_WithColour_QueriesByNormalisedColour(string colour, string expected)
    {
        _repository.ListAsync(expected, Arg.Any<CancellationToken>())
            .Returns([Product.Create("Apple", null, "Red", 1m, Now)]);

        var result = await _sut.ListAsync(colour, CancellationToken.None);

        Assert.Equal("Apple", Assert.Single(result).Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ListAsync_WithoutColour_ReturnsAllProducts(string? colour)
    {
        _repository.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns([Product.Create("A", null, "Red", 1m, Now), Product.Create("B", null, "Blue", 2m, Now)]);

        var result = await _sut.ListAsync(colour, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        Assert.Null(await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
