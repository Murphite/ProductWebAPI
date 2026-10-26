using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Products.Application.Abstractions;
using Products.Domain.Common;
using Products.Domain.Products;
using Products.Infrastructure.Outbox;

namespace Products.Infrastructure.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Persist domain events in the same transaction as the state change (transactional outbox),
        // guaranteeing at-least-once delivery without a distributed transaction.
        AddDomainEventsToOutbox();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductsDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite cannot natively order/compare DateTimeOffset values; store them as sortable, lossless UTC ticks.
        if (Database.IsSqlite())
        {
            configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
            configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcTicksConverter>();
        }
    }

    private void AddDomainEventsToOutbox()
    {
        var entities = ChangeTracker.Entries<Entity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count != 0)
            .ToList();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                OutboxMessages.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    Type = domainEvent.GetType().Name,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), OutboxMessage.SerializerOptions),
                    OccurredAt = domainEvent.OccurredAt,
                });
            }

            entity.ClearDomainEvents();
        }
    }

    private sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
        value => value.UtcTicks,
        ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
}
