//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Messaging;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Extensions;

/// <summary>
/// Phase 24.4 — InboxOutboxExtensions advanced registration (Use*, cleanup, DLQ flags).
/// </summary>
[Trait("Category", "Unit")]
public class InboxOutboxExtensionsAdvancedTest
{
    [Fact]
    public void AddMvpOutbox_WithDeadLetterDisabled_ShouldNotRegisterDeadLetterStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox(o => o.EnableDeadLetterQueue = false);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IDeadLetterStore? store = sp.GetService<IDeadLetterStore>();

        // Assert
        Assert.Null(store);
    }

    [Fact]
    public void AddMvpOutbox_WithDeadLetterEnabled_ShouldRegisterDeadLetterStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox(o => o.EnableDeadLetterQueue = true);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IDeadLetterStore store = sp.GetRequiredService<IDeadLetterStore>();

        // Assert
        Assert.NotNull(store);
    }

    [Fact]
    public void AddMvpOutbox_WithAutomaticCleanup_ShouldRegisterCleanupHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox(o =>
        {
            o.EnableAutomaticCleanup = true;
            o.EnableDeadLetterQueue = false;
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var hosted = sp.GetServices<IHostedService>().ToList();

        // Assert
        Assert.Contains(hosted, h => h.GetType().Name.Contains("OutboxCleanup"));
        Assert.Contains(hosted, h => h.GetType().Name.Contains("OutboxProcessor"));
    }

    [Fact]
    public void AddMvpInboxOutbox_WithCustomOptions_ShouldMapAllFields()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpInboxOutbox(o =>
        {
            o.InboxRetentionDays = 14;
            o.OutboxPollingInterval = TimeSpan.FromSeconds(9);
            o.BatchSize = 50;
            o.MaxRetries = 7;
            o.EnableDeadLetterQueue = true;
            o.EnableAutomaticCleanup = true;
            o.MaxDegreeOfParallelism = 4;
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        InboxOutboxOptions options = sp.GetRequiredService<IOptions<InboxOutboxOptions>>().Value;

        // Assert
        Assert.Equal(14, options.InboxRetentionDays);
        Assert.Equal(TimeSpan.FromSeconds(9), options.OutboxPollingInterval);
        Assert.Equal(50, options.BatchSize);
        Assert.Equal(7, options.MaxRetries);
        Assert.True(options.EnableDeadLetterQueue);
        Assert.True(options.EnableAutomaticCleanup);
        Assert.Equal(4, options.MaxDegreeOfParallelism);
        Assert.NotNull(sp.GetRequiredService<IInboxStore>());
        Assert.NotNull(sp.GetRequiredService<IIntegrationEventOutbox>());
        Assert.NotNull(sp.GetRequiredService<IDeadLetterStore>());
    }

    [Fact]
    public void UseInboxStore_ShouldReplaceDefaultStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpInbox().UseInboxStore<StubInboxStore>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IInboxStore store = sp.GetRequiredService<IInboxStore>();

        // Assert
        Assert.IsType<StubInboxStore>(store);
    }

    [Fact]
    public void UseOutboxStore_ShouldReplaceDefaultStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox().UseOutboxStore<StubOutboxStore>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IIntegrationEventOutbox store = sp.GetRequiredService<IIntegrationEventOutbox>();

        // Assert
        Assert.IsType<StubOutboxStore>(store);
    }

    [Fact]
    public void UseDeadLetterStore_ShouldRegisterCustomStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox(o => o.EnableDeadLetterQueue = true)
            .UseDeadLetterStore<StubDeadLetterStore>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IDeadLetterStore store = sp.GetRequiredService<IDeadLetterStore>();

        // Assert
        Assert.IsType<StubDeadLetterStore>(store);
    }

    [Fact]
    public void UseIntegrationEventPublisher_ShouldRegisterPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpOutbox()
            .UseIntegrationEventPublisher<StubIntegrationEventPublisher>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IIntegrationEventPublisher publisher = sp.GetRequiredService<IIntegrationEventPublisher>();

        // Assert
        Assert.IsType<StubIntegrationEventPublisher>(publisher);
    }

    [Fact]
    public void AddMvpInboxOutbox_WithCleanup_ShouldRegisterBothCleanupServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpInboxOutbox(o => o.EnableAutomaticCleanup = true);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var hosted = sp.GetServices<IHostedService>().Select(h => h.GetType().Name).ToList();

        // Assert
        Assert.Contains(hosted, n => n.Contains("InboxCleanup"));
        Assert.Contains(hosted, n => n.Contains("OutboxCleanup"));
    }
}
