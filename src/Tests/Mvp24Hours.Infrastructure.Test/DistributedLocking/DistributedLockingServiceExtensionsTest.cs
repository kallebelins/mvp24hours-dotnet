//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.DistributedLocking;

[Trait("Category", "Unit")]
public class DistributedLockingServiceExtensionsTest
{
    [Fact]
    public void AddDistributedLocking_WithInMemory_ShouldRegisterFactoryAndMetrics()
    {
        var services = new ServiceCollection();

        services.AddDistributedLocking(builder =>
        {
            builder.AddInMemoryProvider("InMemory");
            builder.SetDefaultProvider("InMemory");
        });

        ServiceProvider sp = services.BuildServiceProvider();

        sp.GetRequiredService<DistributedLockMetrics>().Should().NotBeNull();
        IDistributedLockFactory factory = sp.GetRequiredService<IDistributedLockFactory>();
        factory.Create().Should().NotBeNull();
        factory.Create("InMemory").Should().NotBeNull();
    }

    [Fact]
    public async Task AddDistributedLocking_ResolvedProvider_ShouldAcquireLock()
    {
        var services = new ServiceCollection();
        services.AddDistributedLocking(builder => builder.AddInMemoryProvider());

        ServiceProvider sp = services.BuildServiceProvider();
        IDistributedLock lockProvider = sp.GetRequiredService<IDistributedLockFactory>().Create();
        string resource = DistributedLockingTestHelpers.UniqueResource();

        LockAcquisitionResult result = await lockProvider.TryAcquireAsync(
            resource,
            DistributedLockingTestHelpers.FastFailOptions());

        try
        {
            result.IsAcquired.Should().BeTrue();
        }
        finally
        {
            if (result.LockHandle is not null)
            {
                await result.LockHandle.DisposeAsync();
            }
        }
    }

    [Fact]
    public void AddRedisProvider_WithNullConnection_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddDistributedLocking(builder =>
            builder.AddRedisProvider("Redis", null!));

        act.Should().Throw<ArgumentNullException>().WithParameterName("redisConnection");
    }

    [Fact]
    public void AddRedisProvider_WithConnection_ShouldRegisterProvider()
    {
        DistributedLockingTestHelpers.MockRedis redis = DistributedLockingTestHelpers.CreateMockRedis();
        var services = new ServiceCollection();

        services.AddDistributedLocking(builder =>
        {
            builder.AddRedisProvider("Redis", redis.Object);
            builder.SetDefaultProvider("Redis");
        });

        IDistributedLock provider = services.BuildServiceProvider()
            .GetRequiredService<IDistributedLockFactory>()
            .Create();

        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddRedisRedLockProvider_WithEmptyConnections_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddDistributedLocking(builder =>
            builder.AddRedisRedLockProvider("RedLock", []));

        act.Should().Throw<ArgumentException>().WithParameterName("redisConnections");
    }

    [Fact]
    public void AddRedisRedLockProvider_WithConnections_ShouldRegisterProvider()
    {
        DistributedLockingTestHelpers.MockRedis r1 = DistributedLockingTestHelpers.CreateMockRedis();
        DistributedLockingTestHelpers.MockRedis r2 = DistributedLockingTestHelpers.CreateMockRedis();
        DistributedLockingTestHelpers.MockRedis r3 = DistributedLockingTestHelpers.CreateMockRedis();
        var services = new ServiceCollection();

        services.AddDistributedLocking(builder =>
            builder.AddRedisRedLockProvider("RedLock", [r1.Object, r2.Object, r3.Object]));

        IDistributedLock provider = services.BuildServiceProvider()
            .GetRequiredService<IDistributedLockFactory>()
            .Create("RedLock");

        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddSqlServerProvider_WithInvalidConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddDistributedLocking(builder =>
            builder.AddSqlServerProvider("Sql", "  "));

        act.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }

    [Fact]
    public void AddSqlServerProvider_WithConnectionString_ShouldRegisterProvider()
    {
        var services = new ServiceCollection();

        services.AddDistributedLocking(builder =>
            builder.AddSqlServerProvider(
                "Sql",
                DistributedLockingTestHelpers.UnreachableSqlServerConnectionString()));

        IDistributedLock provider = services.BuildServiceProvider()
            .GetRequiredService<IDistributedLockFactory>()
            .Create("Sql");

        provider.Should().NotBeNull();
    }

    [Fact]
    public void AddPostgreSqlProvider_WithInvalidConnectionString_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddDistributedLocking(builder =>
            builder.AddPostgreSqlProvider("Pg", ""));

        act.Should().Throw<ArgumentException>().WithParameterName("connectionString");
    }

    [Fact]
    public void AddPostgreSqlProvider_WithConnectionString_ShouldRegisterProvider()
    {
        var services = new ServiceCollection();

        services.AddDistributedLocking(builder =>
            builder.AddPostgreSqlProvider(
                "Pg",
                DistributedLockingTestHelpers.UnreachablePostgreSqlConnectionString(),
                useSharedLock: true));

        IDistributedLock provider = services.BuildServiceProvider()
            .GetRequiredService<IDistributedLockFactory>()
            .Create("Pg");

        provider.Should().NotBeNull();
    }

    [Fact]
    public void RegisterProvider_WithEmptyName_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddDistributedLocking(builder =>
            builder.RegisterProvider("  ", _ => new Moq.Mock<IDistributedLock>().Object));

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void AddDistributedLocking_WithoutProviders_ShouldThrowWhenResolvingFactory()
    {
        var services = new ServiceCollection();
        services.AddDistributedLocking();

        Action act = () => _ = services.BuildServiceProvider()
            .GetRequiredService<IDistributedLockFactory>();

        act.Should().Throw<ArgumentException>();
    }
}
