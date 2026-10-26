using System.Text.Json;

namespace Products.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Guid Id { get; init; }

    public required string Type { get; init; }

    public required string Payload { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public int Attempts { get; set; }

    public string? Error { get; set; }
}
