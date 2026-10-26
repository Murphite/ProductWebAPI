using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Products;

namespace Products.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateProductRequestValidator>(includeInternalTypes: true);
        services.AddScoped<IProductService, ProductService>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
