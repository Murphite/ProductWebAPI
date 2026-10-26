using FluentValidation;
using Products.Domain.Products;

namespace Products.Application.Products;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public const decimal MaxPrice = 1_000_000m;

    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(Product.DescriptionMaxLength);

        RuleFor(x => x.Colour)
            .NotEmpty()
            .MaximumLength(Product.ColourMaxLength)
            .Matches("^[\\p{L} \\-]+$")
            .WithMessage("'Colour' may only contain letters, spaces and hyphens.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(MaxPrice)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}
