//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Implementations;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;
using Xunit;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Tests for Inbox/Outbox patterns implementation.
/// </summary>
public class InboxOutboxTest
{
    #region [ Inbox Store Tests ]

    [Fact]
    public async Task InboxStore_MarkAsProcessed_ShouldStoreMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var messageId = Guid.NewGuid();
        var messageType = "TestEvent";

        // Act
        await store.MarkAsProcessedAsync(messageId, messageType);

        // Assert
        var exists = await store.ExistsAsync(messageId);
        Assert.True(exists);
    }

    [Fact]
    public async Task InboxStore_ExistsAsync_ShouldReturnFalseForNewMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var messageId = Guid.NewGuid();

        // Act
        var exists = await store.ExistsAsync(messageId);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task InboxStore_ExistsAsync_ShouldReturnTrueForProcessedMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var messageId = Guid.NewGuid();
        await store.MarkAsProcessedAsync(messageId, "TestEvent");

        // Act
        var exists = await store.ExistsAsync(messageId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task InboxStore_GetByIdAsync_ShouldReturnMessage()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var messageId = Guid.NewGuid();
        var messageType = "TestEvent";
        await store.MarkAsProcessedAsync(messageId, messageType);

        // Act
        var message = await store.GetByIdAsync(messageId);

        // Assert
        Assert.NotNull(message);
        Assert.Equal(messageId, message.Id);
        Assert.Equal(messageType, message.MessageType);
    }

    [Fact]
    public async Task InboxStore_CleanupAsync_ShouldRemoveOldMessages()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var messageId = Guid.NewGuid();
        await store.MarkAsProcessedAsync(messageId, "TestEvent");

        // Act - Cleanup messages older than future date
        var deletedCount = await store.CleanupAsync(DateTime.UtcNow.AddDays(1));

        // Assert
        Assert.Equal(1, deletedCount);
        var exists = await store.ExistsAsync(messageId);
        Assert.False(exists);
    }

    [Fact]
    public async Task InboxStore_GetByTimeRangeAsync_ShouldReturnMessagesInRange()
    {
        // Arrange
        var store = new InMemoryInboxStore();
        var now = DateTime.UtcNow;
        
        await store.MarkAsProcessedAsync(Guid.NewGuid(), "Event1");
        await store.MarkAsProcessedAsync(Guid.NewGuid(), "Event2");

        // Act
        var messages = await store.GetByTimeRangeAsync(
            now.AddMinutes(-1), 
            now.AddMinutes(1));

        // Assert
        Assert.Equal(2, messages.Count);
    }

    #endregion

    #region [ Outbox Store Tests ]

    [Fact]
    public async Task OutboxStore_AddAsync_ShouldStoreMessage()
    {
        // Arrange
        var store = new InMemoryIntegrationEventOutbox();
        var @event = new TestIntegrationEvent { OrderId = 123 };

        // Act
        await store.AddAsync(@event);

        // Assert
        var pending = await store.GetPendingAsync();
        Assert.Single(pending);
    }

    [Fact]
    public async Task OutboxStore_MarkAsPublishedAsync_ShouldUpdateStatus()
    {
        // Arrange
        var store = new InMemoryIntegrationEventOutbox();
        var @event = new TestIntegrationEvent { OrderId = 123 };
        await store.AddAsync(@event);
        var pending = await store.GetPendingAsync();
        var messageId = pending.First().Id;

        // Act
        await store.MarkAsPublishedAsync(messageId);

        // Assert
        var remainingPending = await store.GetPendingAsync();
        Assert.Empty(remainingPending);
    }

    [Fact]
    public async Task OutboxStore_MarkAsFailedAsync_ShouldIncrementRetryCount()
    {
        // Arrange
        var store = new InMemoryIntegrationEventOutbox();
        var @event = new TestIntegrationEvent { OrderId = 123 };
        await store.AddAsync(@event);
        var pending = await store.GetPendingAsync();
        var messageId = pending.First().Id;

        // Act
        await store.MarkAsFailedAsync(messageId, "Test error");

        // Assert
        var stillPending = await store.GetPendingAsync();
        Assert.Single(stillPending);
        Assert.Equal(1, stillPending.First().RetryCount);
    }

    [Fact]
    public async Task OutboxStore_MarkAsFailedAsync_ShouldMoveToDeadLetterAfterMaxRetries()
    {
        // Arrange
        var store = new InMemoryIntegrationEventOutbox();
        var @event = new TestIntegrationEvent { OrderId = 123 };
        await store.AddAsync(@event);
        var pending = await store.GetPendingAsync();
        var messageId = pending.First().Id;

        // Act - Fail 3 times (default max retries)
        await store.MarkAsFailedAsync(messageId, "Error 1");
        await store.MarkAsFailedAsync(messageId, "Error 2");
        await store.MarkAsFailedAsync(messageId, "Error 3");

        // Assert
        var stillPending = await store.GetPendingAsync();
        Assert.Empty(stillPending); // Should be in dead letter, not pending
    }

    #endregion

    #region [ Dead Letter Store Tests ]

    [Fact]
    public async Task DeadLetterStore_AddAsync_ShouldStoreMessage()
    {
        // Arrange
        var store = new InMemoryDeadLetterStore();
        var message = new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "TestEvent",
            Payload = "{}",
            Error = "Test error"
        };

        // Act
        await store.AddAsync(message);

        // Assert
        var count = await store.GetCountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeadLetterStore_GetAllAsync_ShouldReturnPendingMessages()
    {
        // Arrange
        var store = new InMemoryDeadLetterStore();
        await store.AddAsync(new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "Event1",
            Payload = "{}"
        });
        await store.AddAsync(new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "Event2",
            Payload = "{}"
        });

        // Act
        var messages = await store.GetAllAsync();

        // Assert
        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task DeadLetterStore_MarkAsResolvedAsync_ShouldUpdateStatus()
    {
        // Arrange
        var store = new InMemoryDeadLetterStore();
        var message = new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "TestEvent",
            Payload = "{}"
        };
        await store.AddAsync(message);

        // Act
        await store.MarkAsResolvedAsync(message.Id, "Manually fixed");

        // Assert
        var retrieved = await store.GetByIdAsync(message.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(DeadLetterStatus.Resolved, retrieved.Status);
        Assert.Equal("Manually fixed", retrieved.Resolution);
    }

    [Fact]
    public async Task DeadLetterStore_DeleteAsync_ShouldRemoveMessage()
    {
        // Arrange
        var store = new InMemoryDeadLetterStore();
        var message = new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "TestEvent",
            Payload = "{}"
        };
        await store.AddAsync(message);

        // Act
        var deleted = await store.DeleteAsync(message.Id);

        // Assert
        Assert.True(deleted);
        var count = await store.GetCountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeadLetterStore_GetByEventTypeAsync_ShouldFilterMessages()
    {
        // Arrange
        var store = new InMemoryDeadLetterStore();
        await store.AddAsync(new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "OrderEvent",
            Payload = "{}"
        });
        await store.AddAsync(new DeadLetterMessage
        {
            OriginalMessageId = Guid.NewGuid(),
            EventType = "PaymentEvent",
            Payload = "{}"
        });

        // Act
        var orderEvents = await store.GetByEventTypeAsync("OrderEvent");

        // Assert
        Assert.Single(orderEvents);
        Assert.Equal("OrderEvent", orderEvents.First().EventType);
    }

    #endregion

    #region [ Inbox Processor Tests ]

    [Fact]
    public async Task InboxProcessor_ProcessAsync_ShouldProcessNewMessage()
    {
        // Arrange
        var inboxStore = new InMemoryInboxStore();
        var processor = new InboxProcessor(inboxStore);
        var @event = new TestIntegrationEvent { OrderId = 123 };
        var processed = false;

        // Act
        var result = await processor.ProcessAsync(@event, async (e, ct) =>
        {
            processed = true;
            await Task.CompletedTask;
        });

        // Assert
        Assert.True(result);
        Assert.True(processed);
    }

    [Fact]
    public async Task InboxProcessor_ProcessAsync_ShouldSkipDuplicateMessage()
    {
        // Arrange
        var inboxStore = new InMemoryInboxStore();
        var processor = new InboxProcessor(inboxStore);
        var @event = new TestIntegrationEvent { OrderId = 123 };
        var processCount = 0;

        // Act - Process twice
        await processor.ProcessAsync(@event, async (e, ct) =>
        {
            processCount++;
            await Task.CompletedTask;
        });
        
        var result = await processor.ProcessAsync(@event, async (e, ct) =>
        {
            processCount++;
            await Task.CompletedTask;
        });

        // Assert
        Assert.False(result); // Second call should return false (duplicate)
        Assert.Equal(1, processCount); // Should only process once
    }

    [Fact]
    public async Task InboxProcessor_IsDuplicateAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var inboxStore = new InMemoryInboxStore();
        var processor = new InboxProcessor(inboxStore);
        var @event = new TestIntegrationEvent { OrderId = 123 };

        // Act & Assert - Before processing
        var isDuplicateBefore = await processor.IsDuplicateAsync(@event.Id);
        Assert.False(isDuplicateBefore);

        // Process the message
        await processor.ProcessAsync(@event, async (e, ct) => await Task.CompletedTask);

        // Act & Assert - After processing
        var isDuplicateAfter = await processor.IsDuplicateAsync(@event.Id);
        Assert.True(isDuplicateAfter);
    }

    #endregion

    #region [ DI Registration Tests ]

    [Fact]
    public void AddMvpInbox_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMvpInbox();
        var provider = services.BuildServiceProvider();

        // Assert
        var inboxStore = provider.GetService<IInboxStore>();
        var inboxProcessor = provider.GetService<IInboxProcessor>();

        Assert.NotNull(inboxStore);
        Assert.NotNull(inboxProcessor);
    }

    [Fact]
    public void AddMvpOutbox_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMvpOutbox();
        var provider = services.BuildServiceProvider();

        // Assert
        var outbox = provider.GetService<IIntegrationEventOutbox>();
        var dlq = provider.GetService<IDeadLetterStore>();

        Assert.NotNull(outbox);
        Assert.NotNull(dlq);
    }

    [Fact]
    public void AddMvpInboxOutbox_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMvpInboxOutbox();
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetService<IInboxStore>());
        Assert.NotNull(provider.GetService<IIntegrationEventOutbox>());
        Assert.NotNull(provider.GetService<IDeadLetterStore>());
        Assert.NotNull(provider.GetService<IInboxProcessor>());
    }

    #endregion

    #region [ Test Helpers ]

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public int OrderId { get; init; }
    }

    #endregion
}


