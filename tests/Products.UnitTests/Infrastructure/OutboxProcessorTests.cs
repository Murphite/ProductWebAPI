using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Products.Domain.Products;
using Products.Infrastructure.Messaging;
using Products.Infrastructure.Outbox;
using Products.Infrastructure.Persistence;

namespace Products.UnitTests.Infrastructure;

public sealed class OutboxProcessorTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly ProductsDbContext _dbContext;
    private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
    private readonly FakeTimeProvider _timeProvider = new(Now);

    public OutboxProcessorTests()
    {
        _connection.Open();
        _dbContext = new ProductsDbContext(new DbContextOptionsBuilder<ProductsDbContext>().UseSqlite(_connection).Options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task SavingAProduct_WritesProductCreatedMessageToOutbox()
    {
        var product = Product.Create("Mug", null, "White", 5m, Now);

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        var message = Assert.Single(await _dbContext.OutboxMessages.ToListAsync());
        Assert.Equal(nameof(ProductCreatedEvent), message.Type);
        Assert.Contains(product.Id.ToString(), message.Payload, StringComparison.OrdinalIgnoreCase);
        Assert.Null(message.ProcessedAt);
        Assert.Empty(product.DomainEvents);
    }

    [Fact]
    public async Task ProcessBatchAsync_PublishesPendingMessagesAndMarksThemProcessed()
    {
        await SeedProductAsync();

        var processed = await CreateProcessor().ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(1, processed);
        var message = await _dbContext.OutboxMessages.SingleAsync();
        Assert.Equal(Now, message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        await _publisher.Received(1).PublishAsync(nameof(ProductCreatedEvent), message.Id, message.Payload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessBatchAsync_DoesNotRepublishProcessedMessages()
    {
        await SeedProductAsync();
        var processor = CreateProcessor();
        await processor.ProcessBatchAsync(CancellationToken.None);

        var processedSecondRun = await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(0, processedSecondRun);
        await _publisher.ReceivedWithAnyArgs(1).PublishAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenPublishFails_RecordsErrorAndLeavesMessagePending()
    {
        await SeedProductAsync();
        _publisher.PublishAsync(default!, default, default!, default)
            .ThrowsAsyncForAnyArgs(new InvalidOperationException("broker unavailable"));

        await CreateProcessor().ProcessBatchAsync(CancellationToken.None);

        var message = await _dbContext.OutboxMessages.SingleAsync();
        Assert.Null(message.ProcessedAt);
        Assert.Equal(1, message.Attempts);
        Assert.Equal("broker unavailable", message.Error);
    }

    [Fact]
    public async Task ProcessBatchAsync_SkipsMessagesThatExceededMaxAttempts()
    {
        await SeedProductAsync();
        var message = await _dbContext.OutboxMessages.SingleAsync();
        message.Attempts = 3;
        await _dbContext.SaveChangesAsync();

        var processed = await CreateProcessor(maxAttempts: 3).ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(0, processed);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    private async Task SeedProductAsync()
    {
        _dbContext.Products.Add(Product.Create("Mug", null, "White", 5m, Now));
        await _dbContext.SaveChangesAsync();
    }

    private OutboxProcessor CreateProcessor(int maxAttempts = 10) => new(
        _dbContext,
        _publisher,
        _timeProvider,
        Options.Create(new OutboxOptions { MaxAttempts = maxAttempts }),
        NullLogger<OutboxProcessor>.Instance);
}
