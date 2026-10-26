using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Products.Api.Authentication;
using Products.Application.Products;

namespace Products.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ProductsController(IProductService productService) : ControllerBase
{
    /// <summary>Returns all products, optionally filtered by colour (case-insensitive).</summary>
    /// <param name="colour">Optional colour filter, e.g. <c>red</c>.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [Authorize(Policy = Policies.ReadProducts)]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(
        [FromQuery] string? colour,
        CancellationToken cancellationToken)
    {
        var products = await productService.ListAsync(colour, cancellationToken);
        return Ok(products);
    }

    /// <summary>Returns a single product.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.ReadProducts)]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await productService.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Creates a product and publishes a <c>ProductCreated</c> event.</summary>
    [HttpPost]
    [Authorize(Policy = Policies.WriteProducts)]
    [Consumes("application/json")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
}
