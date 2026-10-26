using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Products.Infrastructure.Messaging;
using Products.Infrastructure.Persistence;

namespace Products.Infrastructure.Outbox;

/// <summary>
/// Publishes a batch of pending outbox messages to the message bus and records the outcome.
/// Consumers must be idempotent (keyed on the message id) because delivery is at-least-once.
/// </summary>
public sealed partial class OutboxProcessor(
    ProductsDbContext dbContext,
    IMessagePublisher publisher,
    TimeProvider timeProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor> logger)
{
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.Attempts < settings.MaxAttempts)
            .OrderBy(m => m.OccurredAt)
            .Take(settings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.Type, message.Id, message.Payload, cancellationToken);
                message.ProcessedAt = timeProvider.GetUtcNow();
                message.Error = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogPublishFailed(logger, ex, message.Id, message.Type);
                message.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            }
            finally
            {
                message.Attempts++;
            }
        }

        if (messages.Count != 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish outbox message {MessageId} ({MessageType})")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception, Guid messageId, string messageType);
}
