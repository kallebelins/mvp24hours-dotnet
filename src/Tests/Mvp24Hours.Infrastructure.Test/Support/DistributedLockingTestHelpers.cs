//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Moq;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Providers;
using StackExchange.Redis;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class DistributedLockingTestHelpers
{
    public static string UniqueResource(string prefix = "resource")
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    public static DistributedLockOptions FastFailOptions(
        TimeSpan? acquisitionTimeout = null,
        TimeSpan? lockDuration = null,
        TimeSpan? retryDelay = null,
        bool throwOnFailure = false,
        bool enableAutoRenewal = false,
        TimeSpan? renewalInterval = null)
    {
        return new DistributedLockOptions
        {
            AcquisitionTimeout = acquisitionTimeout ?? TimeSpan.FromMilliseconds(200),
            LockDuration = lockDuration ?? TimeSpan.FromSeconds(30),
            RetryDelay = retryDelay ?? TimeSpan.FromMilliseconds(20),
            ThrowOnFailure = throwOnFailure,
            EnableAutoRenewal = enableAutoRenewal,
            RenewalInterval = renewalInterval ?? TimeSpan.FromMilliseconds(50)
        };
    }

    public static InMemoryDistributedLockProvider CreateInMemory()
    {
        return new();
    }

    public static string UnreachableSqlServerConnectionString()
    {
        return "Server=127.0.0.1,1;Database=Mvp24HoursLockTest;User Id=sa;Password=invalid;Connect Timeout=1;TrustServerCertificate=True;";
    }

    public static string UnreachablePostgreSqlConnectionString()
    {
        return "Host=127.0.0.1;Port=1;Database=Mvp24HoursLockTest;Username=postgres;Password=invalid;Timeout=1;";
    }

    public static MockRedis CreateMockRedis(bool stringSetSucceeds = true)
    {
        var database = new Mock<IDatabase>();
        var multiplexer = new Mock<IConnectionMultiplexer>();

        multiplexer
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(database.Object);

        // Prefer the 4-arg overload used by RedisDistributedLockProvider (no CommandFlags).
        database
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists))
            .ReturnsAsync(stringSetSucceeds);

        database
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                When.NotExists,
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(stringSetSucceeds);

        database
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(stringSetSucceeds ? (RedisValue)"lock-value" : RedisValue.Null);

        database
            .Setup(d => d.ScriptEvaluateAsync(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create(1));

        return new MockRedis(multiplexer, database);
    }

    internal sealed class MockRedis(Mock<IConnectionMultiplexer> multiplexer, Mock<IDatabase> database)
    {
        public Mock<IConnectionMultiplexer> Multiplexer { get; } = multiplexer;
        public Mock<IDatabase> Database { get; } = database;
        public IConnectionMultiplexer Object => Multiplexer.Object;
    }
}
