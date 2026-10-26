using Microsoft.Extensions.Logging;

namespace Products.Infrastructure.Messaging;

/// <summary>
/// Development stand-in for a real broker client: writes events to the log.
/// Replace with a broker-backed implementation (e.g. MassTransit / Azure Service Bus) in deployed environments.
/// </summary>
internal sealed partial class LoggingMessagePublisher(ILogger<LoggingMessagePublisher> logger) : IMessagePublisher
{
    public Task PublishAsync(string messageType, Guid messageId, string payload, CancellationToken cancellationToken)
    {
        LogPublished(logger, messageType, messageId, payload);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published {MessageType} {MessageId}: {Payload}")]
    private static partial void LogPublished(ILogger logger, string messageType, Guid messageId, string payload);
}
