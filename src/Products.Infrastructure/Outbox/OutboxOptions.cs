using System.ComponentModel.DataAnnotations;

namespace Products.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public bool Enabled { get; init; } = true;

    [Range(typeof(TimeSpan), "00:00:00.100", "01:00:00")]
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 50;

    [Range(1, 100)]
    public int MaxAttempts { get; init; } = 10;
}
