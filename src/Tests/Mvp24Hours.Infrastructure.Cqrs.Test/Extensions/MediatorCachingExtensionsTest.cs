//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Caching.Distributed;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Extensions;

/// <summary>
/// Phase 24.4 — MediatorCachingExtensions and MediatorCacheOptions.
/// </summary>
[Trait("Category", "Unit")]
public class MediatorCachingExtensionsTest
{
    [Fact]
    public void AddMediatorMemoryCache_ShouldRegisterIDistributedCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediatorMemoryCache();
        ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(sp.GetRequiredService<IDistributedCache>());
    }

    [Fact]
    public void AddMediatorRedisCache_WithNullConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddMediatorRedisCache(null!));
    }

    [Fact]
    public void AddMediatorRedisCache_WithEmptyConnectionString_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddMediatorRedisCache("   "));
    }

    [Fact]
    public void AddMediatorRedisCache_WithConnectionString_ShouldRegisterDistributedCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediatorRedisCache("localhost:6379", "test:");
        ServiceProvider sp = services.BuildServiceProvider();

        // Assert — registration only; no Redis connection until used
        Assert.NotNull(sp.GetRequiredService<IDistributedCache>());
    }

    [Fact]
    public void AddMediatorRedisCache_WithConfigureAction_ShouldRegisterDistributedCache()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediatorRedisCache(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "cfg:";
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Assert — configure runs lazily when RedisCache options are resolved
        Assert.NotNull(sp.GetRequiredService<IDistributedCache>());
    }

    [Fact]
    public void MediatorCacheOptions_Defaults_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new MediatorCacheOptions();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultQueryCacheDuration);
        Assert.Equal(TimeSpan.FromHours(24), options.DefaultIdempotencyDuration);
        Assert.Equal("mvp24mediator:", options.KeyPrefix);
        Assert.False(options.UseSlidingExpiration);
    }
}
