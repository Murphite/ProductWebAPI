namespace Products.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>
    /// Commits pending changes atomically, including any outbox messages for raised domain events.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
