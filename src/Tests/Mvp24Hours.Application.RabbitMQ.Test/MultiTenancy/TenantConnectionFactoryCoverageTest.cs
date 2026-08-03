using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.MultiTenancy;

[Trait("Category", "Unit")]
public class TenantConnectionFactoryCoverageTest
{
    [Fact]
    public void GetTenantConfiguration_WithResolver_ShouldReturnResolvedConfig()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions());
        var resolver = new InMemoryTenantRabbitMQResolver(Options.Create(new TenantRabbitMQOptions()));
        resolver.AddOrUpdate(new TenantRabbitMQConfiguration
        {
            TenantId = "resolved",
            VirtualHost = "/resolved",
            Username = "user",
            Password = "pass"
        });

        TenantRabbitMQConfiguration? config = InvokeGetTenantConfiguration(factory, "resolved", resolver);

        config.Should().NotBeNull();
        config!.VirtualHost.Should().Be("/resolved");
    }

    [Fact]
    public void CreateConnectionFactory_WithConnectionString_ShouldUseUri()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions(), new RabbitMQConnectionOptions
        {
            ConnectionString = "amqp://guest:guest@localhost:5672",
            RetryCount = 0
        });

        var config = new TenantRabbitMQConfiguration
        {
            TenantId = "tenant-uri",
            ConnectionString = "amqp://tenant:tenant@localhost:5672/vhost"
        };

        ConnectionFactory connectionFactory = InvokeCreateConnectionFactory(factory, "tenant-uri", config);

        connectionFactory.Uri.Should().Be(new Uri("amqp://tenant:tenant@localhost:5672/vhost"));
    }

    [Fact]
    public void CreateConnectionFactory_WithHostConfiguration_ShouldApplyTenantCredentials()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions
        {
            IsolationStrategy = TenantIsolationStrategy.VirtualHostPerTenant
        });

        var config = new TenantRabbitMQConfiguration
        {
            TenantId = "tenant-host",
            VirtualHost = "/tenant-host",
            Username = "tenant-user",
            Password = "tenant-pass"
        };

        ConnectionFactory connectionFactory = InvokeCreateConnectionFactory(factory, "tenant-host", config);

        connectionFactory.HostName.Should().Be("localhost");
        connectionFactory.UserName.Should().Be("tenant-user");
        connectionFactory.Password.Should().Be("tenant-pass");
        connectionFactory.VirtualHost.Should().Be("/tenant-host");
    }

    [Fact]
    public void CreateConnectionFactory_WithoutAnyConfiguration_ShouldThrow()
    {
        using TenantConnectionFactory factory = new(
            Options.Create(new TenantRabbitMQOptions()),
            Options.Create(new RabbitMQConnectionOptions
            {
                ConnectionString = string.Empty,
                Configuration = null
            }));

        Action act = () => InvokeCreateConnectionFactory(factory, "tenant-none", null);

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("*No RabbitMQ connection configuration*");
    }

    [Fact]
    public void OnConnectionShutdown_ShouldRemoveFromPool()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions());
        Mock<IConnection> connectionMock = CreateOpenConnectionMock();
        InjectPooledConnection(factory, "tenant-shutdown", connectionMock.Object);

        factory.HasConnection("tenant-shutdown").Should().BeTrue();

        InvokeOnConnectionShutdown(factory, "tenant-shutdown", new ShutdownEventArgs(ShutdownInitiator.Application, 0, "closed"));

        factory.HasConnection("tenant-shutdown").Should().BeFalse();
    }

    [Fact]
    public void EvictOldestConnection_WhenPoolFull_ShouldCloseOldestTenant()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions { MaxTenantConnections = 1 });
        Mock<IConnection> oldest = CreateOpenConnectionMock();
        InjectPooledConnection(factory, "oldest", oldest.Object, DateTimeOffset.UtcNow.AddHours(-2));

        InvokeEvictOldestConnection(factory);

        factory.HasConnection("oldest").Should().BeFalse();
        oldest.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public void CleanupIdleConnections_ShouldCloseIdleClosedConnections()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions
        {
            IdleConnectionTimeout = TimeSpan.FromMinutes(1)
        });

        Mock<IConnection> idleClosed = CreateOpenConnectionMock(isOpen: false);
        InjectPooledConnection(factory, "idle", idleClosed.Object, DateTimeOffset.UtcNow.AddHours(-2));

        InvokeCleanupIdleConnections(factory);

        factory.HasConnection("idle").Should().BeFalse();
        idleClosed.Verify(c => c.Close(), Times.Once);
    }

    [Fact]
    public void CloseConnection_WhenCloseThrows_ShouldNotRethrow()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions());
        Mock<IConnection> connectionMock = CreateOpenConnectionMock();
        connectionMock.Setup(c => c.Close()).Throws(new InvalidOperationException("close failed"));
        InjectPooledConnection(factory, "tenant-close-error", connectionMock.Object);

        Action act = () => factory.CloseConnection("tenant-close-error");

        act.Should().NotThrow();
        factory.HasConnection("tenant-close-error").Should().BeFalse();
    }

    [Fact]
    public void OnCallbackException_ShouldNotThrow()
    {
        using TenantConnectionFactory factory = CreateFactory(new TenantRabbitMQOptions());

        Action act = () => InvokeOnCallbackException(factory, "tenant-callback", new Exception("callback failed"));

        act.Should().NotThrow();
    }

    private static TenantConnectionFactory CreateFactory(
        TenantRabbitMQOptions tenantOptions,
        RabbitMQConnectionOptions? connectionOptions = null)
    {
        connectionOptions ??= new RabbitMQConnectionOptions
        {
            Configuration = new RabbitMQConnection
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            },
            RetryCount = 0
        };

        return new TenantConnectionFactory(Options.Create(tenantOptions), Options.Create(connectionOptions));
    }

    private static Mock<IConnection> CreateOpenConnectionMock(bool isOpen = true)
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.Setup(c => c.IsOpen).Returns(isOpen);
        return connectionMock;
    }

    private static void InjectPooledConnection(
        TenantConnectionFactory factory,
        string tenantId,
        IConnection connection,
        DateTimeOffset? lastAccessed = null)
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
        entryType.GetProperty("LastAccessed")!.SetValue(entry, lastAccessed ?? DateTimeOffset.UtcNow);

        connections.GetType().GetMethod("TryAdd")!.Invoke(connections, [tenantId, entry]);
    }

    private static TenantRabbitMQConfiguration? InvokeGetTenantConfiguration(
        TenantConnectionFactory factory,
        string tenantId,
        ITenantRabbitMQResolver resolver)
    {
        FieldInfo? resolverField = typeof(TenantConnectionFactory).GetField(
            "_resolver",
            BindingFlags.Instance | BindingFlags.NonPublic);
        resolverField!.SetValue(factory, resolver);

        MethodInfo? method = typeof(TenantConnectionFactory).GetMethod(
            "GetTenantConfiguration",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (TenantRabbitMQConfiguration?)method!.Invoke(factory, [tenantId]);
    }

    private static ConnectionFactory InvokeCreateConnectionFactory(
        TenantConnectionFactory factory,
        string tenantId,
        TenantRabbitMQConfiguration? config)
    {
        MethodInfo? method = typeof(TenantConnectionFactory).GetMethod(
            "CreateConnectionFactory",
            BindingFlags.Instance | BindingFlags.NonPublic);
        return (ConnectionFactory)method!.Invoke(factory, [tenantId, config])!;
    }

    private static void InvokeOnConnectionShutdown(
        TenantConnectionFactory factory,
        string tenantId,
        ShutdownEventArgs args)
    {
        MethodInfo? method = typeof(TenantConnectionFactory).GetMethod(
            "OnConnectionShutdown",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(factory, [tenantId, args]);
    }

    private static void InvokeOnCallbackException(
        TenantConnectionFactory factory,
        string tenantId,
        Exception ex)
    {
        MethodInfo? method = typeof(TenantConnectionFactory).GetMethod(
            "OnCallbackException",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var eventArgs = new CallbackExceptionEventArgs(ex);
        method!.Invoke(factory, [tenantId, eventArgs]);
    }

    private static void InvokeEvictOldestConnection(TenantConnectionFactory factory)
    {
        MethodInfo? method = typeof(TenantConnectionFactory).GetMethod(
            "EvictOldestConnection",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(factory, []);
    }

    private static void InvokeCleanupIdleConnections(TenantConnectionFactory factory)
    {
        MethodInfo? method = typeof(TenantConnectionFactory).GetMethod(
            "CleanupIdleConnections",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method!.Invoke(factory, [null]);
    }
}
