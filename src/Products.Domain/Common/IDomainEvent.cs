namespace Products.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAt { get; }
}
