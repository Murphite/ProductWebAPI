namespace Products.Domain.Common;

/// <summary>
/// Base type for aggregates that raise domain events. Events are collected in memory and
/// dispatched (via the outbox) when the unit of work is committed.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
