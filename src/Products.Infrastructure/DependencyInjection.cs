using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Products.Application.Abstractions;
using Products.Infrastructure.Messaging;
using Products.Infrastructure.Outbox;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ProductsDb";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ProductsDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProductsDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();

        services.AddOptions<OutboxOptions>()
            .Bind(configuration.GetSection(OutboxOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IMessagePublisher, LoggingMessagePublisher>();
        services.AddScoped<OutboxProcessor>();

        if (configuration.GetValue($"{OutboxOptions.SectionName}:{nameof(OutboxOptions.Enabled)}", defaultValue: true))
        {
            services.AddHostedService<OutboxDispatcher>();
        }

        services.AddHealthChecks()
            .AddDbContextCheck<ProductsDbContext>("database", tags: ["ready"]);

        return services;
    }
}
