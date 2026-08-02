//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Extensions;

/// <summary>
/// Phase 24.4 — DomainToIntegrationEventExtensions and AutoIntegrationEventHandler.
/// </summary>
[Trait("Category", "Unit")]
public class DomainToIntegrationEventExtensionsTest
{
    [Fact]
    public void ConvertDomainEventToIntegrationEvent_WithoutConverter_ShouldReturnNull()
    {
        // Arrange
        ServiceProvider sp = new ServiceCollection().AddLogging().BuildServiceProvider();
        var domainEvent = new UserRegisteredEvent { UserId = 1, Email = "a@b.com" };

        // Act
        IIntegrationEvent? result = DomainToIntegrationEventExtensions.ConvertDomainEventToIntegrationEvent(domainEvent, sp);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertDomainEventToIntegrationEvent_WithConverter_ShouldReturnIntegrationEvent()
    {
        // Arrange
        var converter = new UserRegisteredToIntegrationConverter();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<object>(converter);
        ServiceProvider sp = services.BuildServiceProvider();
        var domainEvent = new UserRegisteredEvent { UserId = 42, Email = "user@test.com" };

        // Act
        IIntegrationEvent? result = DomainToIntegrationEventExtensions.ConvertDomainEventToIntegrationEvent(domainEvent, sp);

        // Assert
        Assert.NotNull(result);
        UserRegisteredIntegrationEvent typed = Assert.IsType<UserRegisteredIntegrationEvent>(result);
        Assert.Equal(42, typed.UserId);
        Assert.Equal("user@test.com", typed.Email);
    }

    [Fact]
    public async Task ConvertAndQueueIntegrationEventsAsync_ShouldQueueConvertedEvents()
    {
        // Arrange
        var converter = new UserRegisteredToIntegrationConverter();
        var outbox = new StubOutboxStore();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<object>(converter);
        ServiceProvider sp = services.BuildServiceProvider();
        var entity = new TestAggregate { Id = 7 };
        entity.Register("queue@test.com");

        // Act
        await entity.ConvertAndQueueIntegrationEventsAsync(sp, outbox);

        // Assert
        Assert.Single(outbox.Events);
        Assert.IsType<UserRegisteredIntegrationEvent>(outbox.Events[0]);
    }

    [Fact]
    public async Task ConvertAndQueueIntegrationEventsAsync_WithoutConverter_ShouldNotQueue()
    {
        // Arrange
        var outbox = new StubOutboxStore();
        ServiceProvider sp = new ServiceCollection().AddLogging().BuildServiceProvider();
        var entity = new TestAggregate { Id = 8 };
        entity.Register("noconv@test.com");

        // Act
        await entity.ConvertAndQueueIntegrationEventsAsync(sp, outbox);

        // Assert
        Assert.Empty(outbox.Events);
    }

    [Fact]
    public async Task ConvertAndQueueIntegrationEventsAsync_WithNullEntity_ShouldNoOp()
    {
        // Arrange
        TestAggregate? entity = null;
        var outbox = new StubOutboxStore();
        ServiceProvider sp = new ServiceCollection().BuildServiceProvider();

        // Act
        await entity!.ConvertAndQueueIntegrationEventsAsync(sp, outbox);

        // Assert
        Assert.Empty(outbox.Events);
    }

    [Fact]
    public async Task ConvertAndQueueIntegrationEventsAsync_WithNoDomainEvents_ShouldNoOp()
    {
        // Arrange
        var entity = new TestAggregate { Id = 9 };
        var outbox = new StubOutboxStore();
        ServiceProvider sp = new ServiceCollection().BuildServiceProvider();

        // Act
        await entity.ConvertAndQueueIntegrationEventsAsync(sp, outbox);

        // Assert
        Assert.Empty(outbox.Events);
    }

    [Fact]
    public async Task AutoIntegrationEventHandler_WithConverter_ShouldQueueToOutbox()
    {
        // Arrange
        var outbox = new StubOutboxStore();
        var handler = new AutoIntegrationEventHandler<UserRegisteredEvent, UserRegisteredIntegrationEvent>(
            outbox,
            new UserRegisteredToIntegrationConverter());

        // Act
        await handler.Handle(new UserRegisteredEvent { UserId = 11, Email = "auto@test.com" }, CancellationToken.None);

        // Assert
        Assert.Single(outbox.Events);
        Assert.Equal(11, ((UserRegisteredIntegrationEvent)outbox.Events[0]).UserId);
    }

    [Fact]
    public async Task AutoIntegrationEventHandler_WithoutConverter_ShouldNoOp()
    {
        // Arrange
        var outbox = new StubOutboxStore();
        var handler = new AutoIntegrationEventHandler<UserRegisteredEvent, UserRegisteredIntegrationEvent>(outbox);

        // Act
        await handler.Handle(new UserRegisteredEvent { UserId = 12, Email = "noop@test.com" }, CancellationToken.None);

        // Assert
        Assert.Empty(outbox.Events);
    }

    [Fact]
    public void AutoIntegrationEventHandler_WithNullOutbox_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AutoIntegrationEventHandler<UserRegisteredEvent, UserRegisteredIntegrationEvent>(null!));
    }

    [Fact]
    public void ConvertDomainEventToIntegrationEvent_WithUnrelatedCoreEvent_ShouldReturnNull()
    {
        // Arrange — Core event that is not a Mediator domain event with converter
        CoreDomainEvent coreOnly = new CoreOnlyDomainEvent();
        ServiceProvider sp = new ServiceCollection().AddLogging().BuildServiceProvider();

        // Act
        IIntegrationEvent? result = DomainToIntegrationEventExtensions.ConvertDomainEventToIntegrationEvent(coreOnly, sp);

        // Assert
        Assert.Null(result);
    }

    private sealed class CoreOnlyDomainEvent : CoreDomainEvent
    {
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
        public Guid EventId { get; init; } = Guid.NewGuid();
    }
}
