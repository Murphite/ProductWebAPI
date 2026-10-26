namespace Products.Infrastructure.Messaging;

/// <summary>
/// Abstraction over the message broker (e.g. Azure Service Bus, RabbitMQ, Kafka).
/// </summary>
public interface IMessagePublisher
{
    Task PublishAsync(string messageType, Guid messageId, string payload, CancellationToken cancellationToken);
}
