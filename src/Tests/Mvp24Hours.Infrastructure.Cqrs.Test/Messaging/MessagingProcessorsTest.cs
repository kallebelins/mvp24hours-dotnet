//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Messaging;

[Trait("Category", "Unit")]
public class MessagingProcessorsTest
{
    [Fact]
    public void OutboxProcessor_ShouldBeBackgroundService()
    {
        Assert.True(typeof(BackgroundService).IsAssignableFrom(typeof(OutboxProcessor)));
        Assert.True(typeof(BackgroundService).IsAssignableFrom(typeof(OutboxCleanupService)));
        Assert.True(typeof(BackgroundService).IsAssignableFrom(typeof(InboxCleanupService)));
    }

    [Fact]
    public async Task OutboxProcessor_PublishFailure_ShouldMarkMessageFailed()
    {
        var outbox = new InMemoryIntegrationEventOutbox();
        var publisher = new TestIntegrationEventPublisher(shouldFail: true);
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventOutbox>(outbox);
        services.AddSingleton<IIntegrationEventPublisher>(publisher);
        ServiceProvider sp = services.BuildServiceProvider();

        var processor = new OutboxProcessor(
            sp,
            Options.Create(new InboxOutboxOptions { MaxRetries = 5, BatchSize = 10 }),
            NullLogger<OutboxProcessor>.Instance);

        await outbox.AddAsync(new TestOutboxIntegrationEvent { OrderId = 1 });

        using var cts = new CancellationTokenSource(200);
        await processor.StartAsync(cts.Token);

        try
        {
            await Task.Delay(250);
        }
        finally
        {
            await processor.StopAsync(CancellationToken.None);
        }

        OutboxMessage message = (await outbox.GetPendingAsync()).Single();
        Assert.True(message.RetryCount > 0);
    }

    [Fact]
    public async Task InboxCleanupService_DirectCleanup_ShouldRemoveOldMessages()
    {
        var inbox = new InMemoryInboxStore();
        await inbox.MarkAsProcessedAsync(Guid.NewGuid(), "TestEvent");

        int deleted = await inbox.CleanupAsync(DateTime.UtcNow.AddDays(1));
        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task OutboxCleanupService_DirectCleanup_ShouldRemoveOldMessages()
    {
        var outbox = new InMemoryIntegrationEventOutbox();
        await outbox.AddAsync(new TestOutboxIntegrationEvent { OrderId = 1 });
        OutboxMessage message = (await outbox.GetPendingAsync()).Single();
        await outbox.MarkAsPublishedAsync(message.Id);

        int deleted = await outbox.CleanupAsync(DateTime.UtcNow.AddDays(1));
        Assert.Equal(1, deleted);
    }

    [Fact]
    public async Task RabbitMQOutboxAdapter_AddAndGetPending_ShouldRoundTrip()
    {
        var cqrsOutbox = new InMemoryIntegrationEventOutbox();
        var adapter = new RabbitMQOutboxAdapter(cqrsOutbox, NullLogger<RabbitMQOutboxAdapter>.Instance);
        await adapter.AddAsync(new RabbitMQOutboxMessage
        {
            MessageType = "TestMessage",
            Payload = "{\"value\":1}",
            RoutingKey = "orders.created",
            Exchange = "orders"
        });

        IReadOnlyList<RabbitMQOutboxMessage> pending = await adapter.GetPendingAsync();
        RabbitMQOutboxMessage stored = Assert.Single(pending);

        Assert.Equal("orders.created", stored.RoutingKey);
        Assert.Equal("TestMessage", stored.MessageType);
        Assert.Equal("orders", stored.Exchange);
    }

    [Fact]
    public async Task RabbitMQOutboxAdapter_MarkAsPublished_ShouldRemoveFromPending()
    {
        var cqrsOutbox = new InMemoryIntegrationEventOutbox();
        var adapter = new RabbitMQOutboxAdapter(cqrsOutbox);
        await adapter.AddAsync(new RabbitMQOutboxMessage
        {
            MessageType = "TestMessage",
            Payload = "{}"
        });

        RabbitMQOutboxMessage pendingMessage = Assert.Single(await adapter.GetPendingAsync());
        await adapter.MarkAsPublishedAsync(pendingMessage.Id);

        IReadOnlyList<RabbitMQOutboxMessage> pending = await adapter.GetPendingAsync();
        Assert.Empty(pending);
    }

    [Fact]
    public async Task RabbitMQOutboxAdapter_AddRangeAsync_ShouldAddAllMessages()
    {
        var cqrsOutbox = new InMemoryIntegrationEventOutbox();
        var adapter = new RabbitMQOutboxAdapter(cqrsOutbox);

        await adapter.AddRangeAsync(
        [
            new RabbitMQOutboxMessage { MessageType = "A", Payload = "{}" },
            new RabbitMQOutboxMessage { MessageType = "B", Payload = "{}" }
        ]);

        int count = await adapter.GetPendingCountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public void AddMvpOutbox_WithCleanup_ShouldRegisterHostedServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox(o =>
        {
            o.EnableAutomaticCleanup = true;
            o.EnableDeadLetterQueue = true;
        });

        ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IHostedService> hosted = provider.GetServices<IHostedService>();

        Assert.Contains(hosted, s => s is OutboxProcessor);
        Assert.Contains(hosted, s => s is OutboxCleanupService);
    }

    [Fact]
    public void InboxOutboxOptions_ShouldHaveExpectedDefaults()
    {
        var options = new InboxOutboxOptions();

        Assert.Equal(TimeSpan.FromSeconds(5), options.OutboxPollingInterval);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(5, options.MaxRetries);
        Assert.True(options.EnableAutomaticCleanup);
        Assert.Equal("InboxOutbox", InboxOutboxOptions.SectionName);
    }

    private sealed record TestOutboxIntegrationEvent : IntegrationEventBase
    {
        public int OrderId { get; init; }
    }

    private sealed class TestIntegrationEventPublisher(bool shouldFail = false) : IIntegrationEventPublisher
    {
        public int PublishedCount { get; private set; }

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("publish failed");
            }

            PublishedCount++;
            return Task.CompletedTask;
        }

        public Task PublishFromOutboxAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            if (shouldFail)
            {
                throw new InvalidOperationException("publish failed");
            }

            PublishedCount++;
            return Task.CompletedTask;
        }
    }
}
