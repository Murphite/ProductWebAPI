using Products.Domain.Common;

namespace Products.Domain.Products;

/// <summary>
/// Published to the message bus (via the outbox) so downstream services such as
/// Orders, Inventory and Search can react to new products.
/// </summary>
public sealed record ProductCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ProductId,
    string Name,
    string Colour,
    decimal Price) : IDomainEvent;
