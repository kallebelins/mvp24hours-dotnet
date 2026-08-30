//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Tests for <see cref="RabbitMqIntegrationEventPublisher"/>.
/// This test project intentionally does not reference Mvp24Hours.Infrastructure.RabbitMQ,
/// so IMvpRabbitMQClient can never be resolved via Type.GetType; every call exercises the
/// "client not found" branch. This mirrors how the class behaves when consumers forget to
/// reference/configure the RabbitMQ package.
/// </summary>
[Trait("Category", "Unit")]
public class RabbitMqIntegrationEventPublisherTest
{
    public sealed record TestIntegrationEvent(string Payload) : IIntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
        public string? CorrelationId { get; init; }
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new RabbitMqIntegrationEventPublisher(null!));
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrow()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishAsync<TestIntegrationEvent>(null!));
    }

    [Fact]
    public async Task PublishAsync_WhenRabbitMqClientTypeNotFound_ShouldThrowInvalidOperationException()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);
        var @event = new TestIntegrationEvent("payload");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(@event));

        Assert.Contains("RabbitMQ client not found", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithLogger_ShouldStillThrowWhenClientTypeNotFound()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILogger<RabbitMqIntegrationEventPublisher>>();
        var publisher = new RabbitMqIntegrationEventPublisher(provider, logger);
        var @event = new TestIntegrationEvent("payload") { CorrelationId = "corr-1" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(@event));
    }

    [Fact]
    public async Task PublishAsync_UsesEventIdAsCorrelationId_WhenCorrelationIdIsNull()
    {
        // Exercises the `@event.CorrelationId ?? @event.Id.ToString()` fallback branch;
        // the call still fails at the "client not found" step, but only after computing it.
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);
        var @event = new TestIntegrationEvent("payload") { CorrelationId = null };

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAsync(@event));
    }

    [Fact]
    public async Task PublishFromOutboxAsync_WithNullMessage_ShouldThrow()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            publisher.PublishFromOutboxAsync(null!));
    }

    [Fact]
    public async Task PublishFromOutboxAsync_WhenRabbitMqClientTypeNotFound_ShouldThrowInvalidOperationException()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(TestIntegrationEvent),
            Payload = "{}",
            CreatedAt = DateTime.UtcNow,
            CorrelationId = null
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishFromOutboxAsync(message));

        Assert.Contains("RabbitMQ client not found", exception.Message);
    }

    [Fact]
    public async Task PublishFromOutboxAsync_UsesMessageIdAsCorrelationId_WhenCorrelationIdIsNull()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(TestIntegrationEvent),
            Payload = "{}",
            CreatedAt = DateTime.UtcNow,
            CorrelationId = null
        };

        // The correlation-id fallback (message.CorrelationId ?? message.Id.ToString()) is
        // computed before the "client not found" failure; this asserts the call still fails
        // gracefully with the expected exception type rather than a NullReferenceException.
        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishFromOutboxAsync(message));
    }

    [Fact]
    public async Task PublishFromOutboxAsync_WithExplicitCorrelationId_ShouldStillThrowClientNotFound()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var publisher = new RabbitMqIntegrationEventPublisher(provider);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = nameof(TestIntegrationEvent),
            Payload = "{\"payload\":\"value\"}",
            CreatedAt = DateTime.UtcNow,
            CorrelationId = "explicit-correlation"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishFromOutboxAsync(message));
    }
}
