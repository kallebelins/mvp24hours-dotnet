using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;
using RabbitMQ.Client;

namespace Mvp24Hours.Application.RabbitMQ.Test.MultiTenancy;

[Trait("Category", "Unit")]
public class MultiTenancyTest
{
    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_ResolveAsync_ShouldReturnDefaultConfiguration()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));

        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync("tenant-a");

        config.Should().NotBeNull();
        config!.TenantId.Should().Be("tenant-a");
        config.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_AddOrUpdate_ShouldOverrideStaticConfig()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));
        resolver.AddOrUpdate(new TenantRabbitMQConfiguration
        {
            TenantId = "tenant-b",
            VirtualHost = "/custom",
            QueuePrefix = "custom-"
        });

        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync("tenant-b");

        config!.VirtualHost.Should().Be("/custom");
        config.QueuePrefix.Should().Be("custom-");
    }

    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_WithEmptyTenantId_ShouldReturnNull()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));

        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync(string.Empty);

        config.Should().BeNull();
    }

    [Fact]
    public void TenantRabbitMQOptions_GetVirtualHost_ShouldApplyTemplate()
    {
        var options = new TenantRabbitMQOptions
        {
            VirtualHostTemplate = "/tenants/{tenantId}"
        };

        options.GetVirtualHost("acme").Should().Be("/tenants/acme");
        options.GetQueuePrefix("acme").Should().Contain("acme");
    }

    [Fact]
    public async Task TenantConsumeFilter_WithoutTenantAndRejectEnabled_ShouldSendToDeadLetter()
    {
        IOptions<TenantRabbitMQOptions> options = Options.Create(new TenantRabbitMQOptions { RejectMessagesWithoutTenant = true });
        var filter = new TenantConsumeFilter(options);
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeTrue();
    }

    [Fact]
    public async Task TenantConsumeFilter_WithTenantHeader_ShouldSetCurrentContext()
    {
        IOptions<TenantRabbitMQOptions> options = Options.Create(new TenantRabbitMQOptions { RejectMessagesWithoutTenant = false });
        var filter = new TenantConsumeFilter(options);
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.ForTenant("tenant-42"));

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            TenantConsumeFilter.Current!.TenantId.Should().Be("tenant-42");
            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task TenantPublishFilter_WithAutoPropagateDisabled_ShouldNotAddHeaders()
    {
        var services = new ServiceCollection();
        IServiceProvider provider = services.BuildServiceProvider();
        IOptions<TenantRabbitMQOptions> options = Options.Create(new TenantRabbitMQOptions { AutoPropagateTenantHeaders = false });
        var filter = new TenantPublishFilter(options, provider);
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent(), serviceProvider: provider);

        await filter.PublishAsync(context, async (_, _) => await Task.CompletedTask);

        context.Headers.Should().NotContainKey(options.Value.TenantIdHeader);
    }

    [Fact]
    public void TenantRabbitMQOptions_DefaultValues_ShouldHaveExpectedDefaults()
    {
        var options = new TenantRabbitMQOptions();

        options.RejectMessagesWithoutTenant.Should().BeFalse();
        options.AutoPropagateTenantHeaders.Should().BeTrue();
        options.TenantIdHeader.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TenantRabbitMQOptions_GetQueuePrefix_WithTemplate_ShouldFormatCorrectly()
    {
        var options = new TenantRabbitMQOptions
        {
            QueuePrefixTemplate = "tnt_{tenantId}_"
        };

        options.GetQueuePrefix("acme").Should().Be("tnt_acme_");
    }

    [Fact]
    public void TenantRabbitMQConfiguration_ShouldHaveDefaultValues()
    {
        var config = new TenantRabbitMQConfiguration();

        config.IsEnabled.Should().BeTrue();
        config.TenantId.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_Remove_ShouldRemoveConfiguration()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));
        resolver.AddOrUpdate(new TenantRabbitMQConfiguration
        {
            TenantId = "removable",
            VirtualHost = "/test"
        });

        resolver.Remove("removable");
        TenantRabbitMQConfiguration? config = await resolver.ResolveAsync("removable");

        config!.VirtualHost.Should().NotBe("/test");
    }

    [Fact]
    public async Task InMemoryTenantRabbitMQResolver_MultipleResolves_ShouldReturnConsistentConfig()
    {
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));
        resolver.AddOrUpdate(new TenantRabbitMQConfiguration
        {
            TenantId = "consistent",
            QueuePrefix = "cons_"
        });

        TenantRabbitMQConfiguration? first = await resolver.ResolveAsync("consistent");
        TenantRabbitMQConfiguration? second = await resolver.ResolveAsync("consistent");

        first!.QueuePrefix.Should().Be(second!.QueuePrefix);
    }

    [Fact]
    public async Task TenantConsumeFilter_WithoutRejectEnabled_ShouldCallNext()
    {
        IOptions<TenantRabbitMQOptions> options = Options.Create(new TenantRabbitMQOptions { RejectMessagesWithoutTenant = false });
        var filter = new TenantConsumeFilter(options);
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void TenantConnectionFactory_HasConnection_WhenPoolEmpty_ShouldReturnFalse()
    {
        using TenantConnectionFactory factory = CreateTenantConnectionFactory();

        factory.HasConnection("tenant-a").Should().BeFalse();
    }

    [Fact]
    public void TenantConnectionFactory_GetOrCreateConnection_PoolHit_ShouldReturnSameInstance()
    {
        using TenantConnectionFactory factory = CreateTenantConnectionFactory();
        Mock<IConnection> connectionMock = CreateOpenConnectionMock();
        InjectPooledConnection(factory, "tenant-a", connectionMock.Object);

        IConnection first = factory.GetOrCreateConnection("tenant-a");
        IConnection second = factory.GetOrCreateConnection("tenant-a");

        first.Should().BeSameAs(second);
        first.Should().BeSameAs(connectionMock.Object);
    }

    [Fact]
    public void TenantConnectionFactory_GetOrCreateConnection_PoolMissWithClosedConnection_ShouldRemoveStaleEntry()
    {
        using TenantConnectionFactory factory = CreateTenantConnectionFactory();
        Mock<IConnection> closedConnection = CreateOpenConnectionMock(isOpen: false);
        InjectPooledConnection(factory, "tenant-a", closedConnection.Object);

        factory.HasConnection("tenant-a").Should().BeFalse();

        Action act = () => factory.GetOrCreateConnection("tenant-a");

        act.Should().Throw<Exception>();
    }

    [Fact]
    public void TenantConnectionFactory_CloseConnection_ShouldRemoveFromPool()
    {
        using TenantConnectionFactory factory = CreateTenantConnectionFactory();
        Mock<IConnection> connectionMock = CreateOpenConnectionMock();
        InjectPooledConnection(factory, "tenant-b", connectionMock.Object);

        factory.CloseConnection("tenant-b");

        factory.HasConnection("tenant-b").Should().BeFalse();
        connectionMock.Verify(c => c.Close(), Times.Once);
        connectionMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void TenantConnectionFactory_Dispose_ShouldCloseAllPooledConnections()
    {
        TenantConnectionFactory factory = CreateTenantConnectionFactory();
        Mock<IConnection> connectionMock = CreateOpenConnectionMock();
        InjectPooledConnection(factory, "tenant-c", connectionMock.Object);

        factory.Dispose();

        connectionMock.Verify(c => c.Close(), Times.Once);
        connectionMock.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void TenantConnectionFactory_GetVirtualHost_ShouldApplyTemplate()
    {
        using TenantConnectionFactory factory = CreateTenantConnectionFactory(new TenantRabbitMQOptions
        {
            VirtualHostTemplate = "/tenants/{tenantId}"
        });

        factory.GetVirtualHost("acme").Should().Be("/tenants/acme");
    }

    private static TenantConnectionFactory CreateTenantConnectionFactory(TenantRabbitMQOptions? tenantOptions = null)
    {
        tenantOptions ??= new TenantRabbitMQOptions();
        IOptions<TenantRabbitMQOptions> tenantOptionsWrapper = Options.Create(tenantOptions);
        IOptions<RabbitMQConnectionOptions> connectionOptions = Options.Create(new RabbitMQConnectionOptions
        {
            Configuration = new RabbitMQConnection
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            },
            RetryCount = 0
        });

        return new TenantConnectionFactory(tenantOptionsWrapper, connectionOptions);
    }

    private static Mock<IConnection> CreateOpenConnectionMock(bool isOpen = true)
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.Setup(c => c.IsOpen).Returns(isOpen);
        return connectionMock;
    }

    private static void InjectPooledConnection(TenantConnectionFactory factory, string tenantId, IConnection connection)
    {
        FieldInfo? connectionsField = typeof(TenantConnectionFactory).GetField(
            "_connections",
            BindingFlags.Instance | BindingFlags.NonPublic);
        connectionsField.Should().NotBeNull();

        object connections = connectionsField!.GetValue(factory)!;
        Type entryType = typeof(TenantConnectionFactory).GetNestedType("TenantConnectionEntry", BindingFlags.NonPublic)!;
        object entry = Activator.CreateInstance(entryType)!;
        entryType.GetProperty("TenantId")!.SetValue(entry, tenantId);
        entryType.GetProperty("Connection")!.SetValue(entry, connection);
        entryType.GetProperty("CreatedAt")!.SetValue(entry, DateTimeOffset.UtcNow);
        entryType.GetProperty("LastAccessed")!.SetValue(entry, DateTimeOffset.UtcNow);

        MethodInfo tryAdd = connections.GetType().GetMethod("TryAdd")!;
        bool added = (bool)tryAdd.Invoke(connections, [tenantId, entry])!;
        added.Should().BeTrue();
    }
}
