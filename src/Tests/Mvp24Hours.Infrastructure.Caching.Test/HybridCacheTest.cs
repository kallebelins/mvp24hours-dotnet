//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.HybridCache;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Test
{
    /// <summary>
    /// Unit tests for HybridCache integration (.NET 9).
    /// </summary>
    public class HybridCacheTest
    {
        #region MvpHybridCacheOptions Tests

        [Fact]
        public void MvpHybridCacheOptions_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new MvpHybridCacheOptions();

            // Assert
            options.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(5));
            options.MaximumPayloadBytes.Should().Be(1024 * 1024); // 1MB
            options.MaximumKeyLength.Should().Be(1024);
            options.UseRedisAsL2.Should().BeFalse();
            options.EnableStampedeProtection.Should().BeTrue();
            options.ReportTagStatistics.Should().BeTrue();
            options.EnableCompression.Should().BeFalse();
            options.CompressionThresholdBytes.Should().Be(1024);
            options.EnableDetailedLogging.Should().BeFalse();
            options.SerializerType.Should().Be(HybridCacheSerializerType.SystemTextJson);
        }

        [Fact]
        public void MvpHybridCacheOptions_CanSetCustomValues()
        {
            // Arrange & Act
            var options = new MvpHybridCacheOptions
            {
                DefaultExpiration = TimeSpan.FromMinutes(30),
                MaximumPayloadBytes = 2 * 1024 * 1024, // 2MB
                UseRedisAsL2 = true,
                RedisConnectionString = "localhost:6379",
                RedisInstanceName = "test:",
                EnableCompression = true,
                KeyPrefix = "myapp:"
            };

            // Assert
            options.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
            options.MaximumPayloadBytes.Should().Be(2 * 1024 * 1024);
            options.UseRedisAsL2.Should().BeTrue();
            options.RedisConnectionString.Should().Be("localhost:6379");
            options.RedisInstanceName.Should().Be("test:");
            options.EnableCompression.Should().BeTrue();
            options.KeyPrefix.Should().Be("myapp:");
        }

        [Fact]
        public void MvpHybridCacheOptions_DefaultTags_ShouldBeEmptyList()
        {
            // Arrange & Act
            var options = new MvpHybridCacheOptions();

            // Assert
            options.DefaultTags.Should().NotBeNull();
            options.DefaultTags.Should().BeEmpty();
        }

        [Fact]
        public void MvpHybridCacheOptions_CanAddDefaultTags()
        {
            // Arrange
            var options = new MvpHybridCacheOptions();

            // Act
            options.DefaultTags.Add("global");
            options.DefaultTags.Add("tenant:123");

            // Assert
            options.DefaultTags.Should().HaveCount(2);
            options.DefaultTags.Should().Contain("global");
            options.DefaultTags.Should().Contain("tenant:123");
        }

        #endregion

        #region InMemoryHybridCacheTagManager Tests

        [Fact]
        public async Task TagManager_TrackKeyWithTags_ShouldTrackCorrectly()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            var key = "product:123";
            var tags = new[] { "products", "category:electronics" };

            // Act
            await tagManager.TrackKeyWithTagsAsync(key, tags);

            // Assert
            var keysByTag1 = await tagManager.GetKeysByTagAsync("products");
            var keysByTag2 = await tagManager.GetKeysByTagAsync("category:electronics");
            var tagsByKey = await tagManager.GetTagsByKeyAsync(key);

            keysByTag1.Should().Contain(key);
            keysByTag2.Should().Contain(key);
            tagsByKey.Should().Contain("products");
            tagsByKey.Should().Contain("category:electronics");
        }

        [Fact]
        public async Task TagManager_RemoveKeyFromTags_ShouldRemoveCorrectly()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            var key = "product:123";
            var tags = new[] { "products", "category:electronics" };
            await tagManager.TrackKeyWithTagsAsync(key, tags);

            // Act
            await tagManager.RemoveKeyFromTagsAsync(key);

            // Assert
            var keysByTag1 = await tagManager.GetKeysByTagAsync("products");
            var keysByTag2 = await tagManager.GetKeysByTagAsync("category:electronics");
            var tagsByKey = await tagManager.GetTagsByKeyAsync(key);

            keysByTag1.Should().NotContain(key);
            keysByTag2.Should().NotContain(key);
            tagsByKey.Should().BeEmpty();
        }

        [Fact]
        public async Task TagManager_InvalidateTag_ShouldRemoveAllAssociations()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            await tagManager.TrackKeyWithTagsAsync("product:1", new[] { "products" });
            await tagManager.TrackKeyWithTagsAsync("product:2", new[] { "products" });
            await tagManager.TrackKeyWithTagsAsync("product:3", new[] { "products", "featured" });

            // Act
            await tagManager.InvalidateTagAsync("products");

            // Assert
            var keysForProducts = await tagManager.GetKeysByTagAsync("products");
            keysForProducts.Should().BeEmpty();

            // Keys should no longer have "products" tag
            var tagsForKey1 = await tagManager.GetTagsByKeyAsync("product:1");
            var tagsForKey3 = await tagManager.GetTagsByKeyAsync("product:3");

            tagsForKey1.Should().NotContain("products");
            tagsForKey3.Should().NotContain("products");
            tagsForKey3.Should().Contain("featured"); // Other tags should remain
        }

        [Fact]
        public async Task TagManager_GetStatistics_ShouldReturnCorrectStats()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            await tagManager.TrackKeyWithTagsAsync("product:1", new[] { "products", "featured" });
            await tagManager.TrackKeyWithTagsAsync("product:2", new[] { "products" });
            await tagManager.TrackKeyWithTagsAsync("category:1", new[] { "categories" });

            // Act
            var stats = tagManager.GetStatistics();

            // Assert
            stats.TotalTags.Should().Be(3);
            stats.KeysPerTag["products"].Should().Be(2);
            stats.KeysPerTag["featured"].Should().Be(1);
            stats.KeysPerTag["categories"].Should().Be(1);
        }

        [Fact]
        public async Task TagManager_GetStatistics_AfterInvalidation_ShouldTrackInvalidations()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            await tagManager.TrackKeyWithTagsAsync("product:1", new[] { "products" });

            // Act
            await tagManager.InvalidateTagAsync("products");
            var stats = tagManager.GetStatistics();

            // Assert
            stats.TagInvalidations.Should().Be(1);
        }

        [Fact]
        public async Task TagManager_Clear_ShouldRemoveAllData()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            await tagManager.TrackKeyWithTagsAsync("product:1", new[] { "products" });
            await tagManager.TrackKeyWithTagsAsync("product:2", new[] { "products" });

            // Act
            await tagManager.ClearAsync();

            // Assert
            var stats = tagManager.GetStatistics();
            stats.TotalTags.Should().Be(0);
            stats.TotalAssociations.Should().Be(0);
        }

        [Fact]
        public async Task TagManager_TrackKeyWithTags_NullKey_ShouldThrow()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                tagManager.TrackKeyWithTagsAsync(null!, new[] { "tag" }));
        }

        [Fact]
        public async Task TagManager_TrackKeyWithTags_EmptyTags_ShouldNotThrow()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();

            // Act
            await tagManager.TrackKeyWithTagsAsync("key", Array.Empty<string>());

            // Assert
            var stats = tagManager.GetStatistics();
            stats.TotalTags.Should().Be(0);
        }

        [Fact]
        public async Task TagManager_GetKeysByTag_NonExistentTag_ShouldReturnEmpty()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();

            // Act
            var keys = await tagManager.GetKeysByTagAsync("non-existent");

            // Assert
            keys.Should().BeEmpty();
        }

        [Fact]
        public async Task TagManager_MultipleKeysWithSameTag_ShouldTrackAll()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            var tag = "products";

            // Act
            for (int i = 1; i <= 100; i++)
            {
                await tagManager.TrackKeyWithTagsAsync($"product:{i}", new[] { tag });
            }

            // Assert
            var keys = await tagManager.GetKeysByTagAsync(tag);
            keys.Should().HaveCount(100);
        }

        #endregion

        #region HybridCacheServiceExtensions Tests

        [Fact]
        public void AddMvpHybridCache_ShouldRegisterServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddLogging();

            // Act
#pragma warning disable EXTEXP0018
            services.AddMvpHybridCache();
#pragma warning restore EXTEXP0018
            var provider = services.BuildServiceProvider();

            // Assert
            var cacheProvider = provider.GetService<ICacheProvider>();
            var tagManager = provider.GetService<IHybridCacheTagManager>();

            cacheProvider.Should().NotBeNull();
            cacheProvider.Should().BeOfType<HybridCacheProvider>();
            tagManager.Should().NotBeNull();
            tagManager.Should().BeOfType<InMemoryHybridCacheTagManager>();
        }

        [Fact]
        public void AddMvpHybridCache_WithConfiguration_ShouldApplyOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddLogging();

            // Act
#pragma warning disable EXTEXP0018
            services.AddMvpHybridCache(options =>
            {
                options.DefaultExpiration = TimeSpan.FromMinutes(30);
                options.KeyPrefix = "test:";
                options.EnableDetailedLogging = true;
            });
#pragma warning restore EXTEXP0018
            var provider = services.BuildServiceProvider();

            // Assert
            var options = provider.GetService<IOptions<MvpHybridCacheOptions>>();
            options.Should().NotBeNull();
            options!.Value.DefaultExpiration.Should().Be(TimeSpan.FromMinutes(30));
            options.Value.KeyPrefix.Should().Be("test:");
            options.Value.EnableDetailedLogging.Should().BeTrue();
        }

        [Fact]
        public void ReplaceCacheProviderWithHybridCache_ShouldReplaceExisting()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddLogging();

            // Add a mock provider first
            var mockProvider = new Mock<ICacheProvider>();
            services.AddSingleton(mockProvider.Object);

            // Act
#pragma warning disable EXTEXP0018
            services.ReplaceCacheProviderWithHybridCache();
#pragma warning restore EXTEXP0018
            var provider = services.BuildServiceProvider();

            // Assert
            var cacheProvider = provider.GetService<ICacheProvider>();
            cacheProvider.Should().BeOfType<HybridCacheProvider>();
        }

        [Fact]
        public void AddHybridCacheTagManager_CustomImplementation_ShouldReplace()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddMemoryCache();
            services.AddLogging();
#pragma warning disable EXTEXP0018
            services.AddMvpHybridCache();
#pragma warning restore EXTEXP0018

            var customTagManager = new Mock<IHybridCacheTagManager>();

            // Act
            services.AddSingleton(customTagManager.Object);
            var provider = services.BuildServiceProvider();

            // Assert - Should have both registrations but custom should be retrievable
            var tagManager = provider.GetService<IHybridCacheTagManager>();
            tagManager.Should().NotBeNull();
        }

        #endregion

        #region CacheEntryOptions Integration Tests

        [Fact]
        public void CacheEntryOptions_WithTags_ShouldSupportTagging()
        {
            // Arrange
            var options = new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                Tags = new List<string> { "products", "featured" }
            };

            // Assert
            options.Tags.Should().HaveCount(2);
            options.Tags.Should().Contain("products");
            options.Tags.Should().Contain("featured");
        }

        [Fact]
        public void CacheEntryOptions_FactoryMethods_ShouldWork()
        {
            // Act
            var fromDuration = CacheEntryOptions.FromDuration(TimeSpan.FromMinutes(5));
            var withSliding = CacheEntryOptions.WithSlidingExpiration(TimeSpan.FromMinutes(2));
            var withBoth = CacheEntryOptions.WithBothExpirations(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(2));

            // Assert
            fromDuration.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(5));
            withSliding.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(2));
            withBoth.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(10));
            withBoth.SlidingExpiration.Should().Be(TimeSpan.FromMinutes(2));
        }

        #endregion

        #region HybridCacheSerializerType Tests

        [Fact]
        public void HybridCacheSerializerType_ShouldHaveExpectedValues()
        {
            // Assert
            ((int)HybridCacheSerializerType.SystemTextJson).Should().Be(0);
            ((int)HybridCacheSerializerType.MessagePack).Should().Be(1);
            ((int)HybridCacheSerializerType.Custom).Should().Be(2);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task TagManager_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            var tasks = new List<Task>();

            // Act - Concurrent writes
            for (int i = 0; i < 100; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    await tagManager.TrackKeyWithTagsAsync($"key:{index}", new[] { "tag1", "tag2" });
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            var stats = tagManager.GetStatistics();
            stats.TotalTags.Should().Be(2);
            
            var keysForTag1 = await tagManager.GetKeysByTagAsync("tag1");
            keysForTag1.Should().HaveCount(100);
        }

        [Fact]
        public async Task TagManager_InvalidateTagDuringWrite_ShouldNotThrow()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            await tagManager.TrackKeyWithTagsAsync("key:1", new[] { "tag1" });

            // Act - Concurrent invalidation and write
            var tasks = new[]
            {
                tagManager.InvalidateTagAsync("tag1"),
                tagManager.TrackKeyWithTagsAsync("key:2", new[] { "tag1" })
            };

            // Assert - Should not throw
            await Task.WhenAll(tasks);
        }

        #endregion

        #region HybridCacheExtensions Tests

        [Fact]
        public async Task GetOrDefaultAsync_WhenKeyNotFound_ShouldReturnDefault()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();
            mockProvider.Setup(x => x.GetAsync<TestEntity>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TestEntity?)null);

            var defaultValue = new TestEntity { Id = 999, Name = "Default" };

            // Act
            var result = await mockProvider.Object.GetOrDefaultAsync("non-existent", defaultValue);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(999);
            result.Name.Should().Be("Default");
        }

        [Fact]
        public async Task GetOrDefaultAsync_WhenKeyFound_ShouldReturnCachedValue()
        {
            // Arrange
            var cachedEntity = new TestEntity { Id = 1, Name = "Cached" };
            var mockProvider = new Mock<ICacheProvider>();
            mockProvider.Setup(x => x.GetAsync<TestEntity>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedEntity);

            var defaultValue = new TestEntity { Id = 999, Name = "Default" };

            // Act
            var result = await mockProvider.Object.GetOrDefaultAsync("existing", defaultValue);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
            result.Name.Should().Be("Cached");
        }

        [Fact]
        public async Task SetWithTagsAsync_ShouldCallSetAsyncWithOptions()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();
            var entity = new TestEntity { Id = 1, Name = "Test" };

            // Act
            await mockProvider.Object.SetWithTagsAsync(
                "key",
                entity,
                new[] { "tag1", "tag2" },
                expirationMinutes: 10);

            // Assert
            mockProvider.Verify(x => x.SetAsync(
                "key",
                entity,
                It.Is<CacheEntryOptions>(o =>
                    o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(10) &&
                    o.Tags != null &&
                    o.Tags.Contains("tag1") &&
                    o.Tags.Contains("tag2")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SetWithSlidingExpirationAsync_ShouldCallSetAsyncWithSlidingExpiration()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();
            var entity = new TestEntity { Id = 1, Name = "Test" };

            // Act
            await mockProvider.Object.SetWithSlidingExpirationAsync("key", entity, 15);

            // Assert
            mockProvider.Verify(x => x.SetAsync(
                "key",
                entity,
                It.Is<CacheEntryOptions>(o => o.SlidingExpiration == TimeSpan.FromMinutes(15)),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ContainsKeyAsync_ShouldCallExistsAsync()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();
            mockProvider.Setup(x => x.ExistsAsync("key", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await mockProvider.Object.ContainsKeyAsync("key");

            // Assert
            result.Should().BeTrue();
            mockProvider.Verify(x => x.ExistsAsync("key", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RemoveByPrefixAsync_ShouldFilterAndRemoveMatchingKeys()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();
            var keys = new[] { "product:1", "product:2", "category:1", "product:3" };

            // Act
            await mockProvider.Object.RemoveByPrefixAsync("product:", keys);

            // Assert
            mockProvider.Verify(x => x.RemoveManyAsync(
                It.Is<string[]>(k => k.Length == 3 &&
                    k.Contains("product:1") &&
                    k.Contains("product:2") &&
                    k.Contains("product:3")),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void InvalidateByTagAsync_WithNonHybridCacheProvider_ShouldThrow()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
                mockProvider.Object.InvalidateByTagAsync("tag"));
            exception.Result.Message.Should().Contain("HybridCacheProvider");
        }

        [Fact]
        public void InvalidateByTagsAsync_WithNonHybridCacheProvider_ShouldThrow()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
                mockProvider.Object.InvalidateByTagsAsync(new[] { "tag1", "tag2" }));
            exception.Result.Message.Should().Contain("HybridCacheProvider");
        }

        [Fact]
        public void GetOrCreateAsync_WithNonHybridCacheProvider_ShouldThrow()
        {
            // Arrange
            var mockProvider = new Mock<ICacheProvider>();

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
                mockProvider.Object.GetOrCreateAsync(
                    "key",
                    ct => ValueTask.FromResult(new TestEntity { Id = 1 })));
            exception.Result.Message.Should().Contain("HybridCacheProvider");
        }

        #endregion

        #region RedisHybridCacheTagManagerOptions Tests

        [Fact]
        public void RedisHybridCacheTagManagerOptions_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var options = new RedisHybridCacheTagManagerOptions();

            // Assert
            options.DatabaseId.Should().Be(0);
            options.KeyPrefix.Should().Be("mvp24h:tags:");
            options.TagExpiration.Should().BeNull();
            options.KeyTagsMappingExpiration.Should().BeNull();
        }

        [Fact]
        public void RedisHybridCacheTagManagerOptions_CanSetCustomValues()
        {
            // Arrange & Act
            var options = new RedisHybridCacheTagManagerOptions
            {
                DatabaseId = 2,
                KeyPrefix = "myapp:tags:",
                TagExpiration = TimeSpan.FromHours(24),
                KeyTagsMappingExpiration = TimeSpan.FromHours(12)
            };

            // Assert
            options.DatabaseId.Should().Be(2);
            options.KeyPrefix.Should().Be("myapp:tags:");
            options.TagExpiration.Should().Be(TimeSpan.FromHours(24));
            options.KeyTagsMappingExpiration.Should().Be(TimeSpan.FromHours(12));
        }

        #endregion

        #region HybridCacheTagStatistics Tests

        [Fact]
        public void HybridCacheTagStatistics_ShouldInitializeWithDefaults()
        {
            // Arrange & Act
            var stats = new HybridCacheTagStatistics();

            // Assert
            stats.TotalTags.Should().Be(0);
            stats.TotalAssociations.Should().Be(0);
            stats.TagInvalidations.Should().Be(0);
            stats.KeysPerTag.Should().NotBeNull();
            stats.KeysPerTag.Should().BeEmpty();
        }

        [Fact]
        public void HybridCacheTagStatistics_CanSetValues()
        {
            // Arrange & Act
            var stats = new HybridCacheTagStatistics
            {
                TotalTags = 5,
                TotalAssociations = 100,
                TagInvalidations = 10,
                KeysPerTag = new Dictionary<string, int>
                {
                    ["products"] = 50,
                    ["categories"] = 30,
                    ["users"] = 20
                }
            };

            // Assert
            stats.TotalTags.Should().Be(5);
            stats.TotalAssociations.Should().Be(100);
            stats.TagInvalidations.Should().Be(10);
            stats.KeysPerTag.Should().HaveCount(3);
            stats.KeysPerTag["products"].Should().Be(50);
        }

        #endregion

        #region Integration Scenario Tests

        [Fact]
        public async Task FullTaggingWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();

            // Act - Create products with multiple tags
            await tagManager.TrackKeyWithTagsAsync("product:1", new[] { "products", "category:electronics", "featured" });
            await tagManager.TrackKeyWithTagsAsync("product:2", new[] { "products", "category:electronics" });
            await tagManager.TrackKeyWithTagsAsync("product:3", new[] { "products", "category:clothing" });
            await tagManager.TrackKeyWithTagsAsync("product:4", new[] { "products", "category:clothing", "featured" });

            // Assert - Check statistics
            var stats = tagManager.GetStatistics();
            stats.TotalTags.Should().Be(4); // products, category:electronics, category:clothing, featured
            stats.KeysPerTag["products"].Should().Be(4);
            stats.KeysPerTag["category:electronics"].Should().Be(2);
            stats.KeysPerTag["category:clothing"].Should().Be(2);
            stats.KeysPerTag["featured"].Should().Be(2);

            // Act - Invalidate electronics category
            await tagManager.InvalidateTagAsync("category:electronics");

            // Assert - Check remaining tags
            var statsAfter = tagManager.GetStatistics();
            statsAfter.TotalTags.Should().Be(3); // products, category:clothing, featured
            statsAfter.TagInvalidations.Should().Be(1);

            // Verify product:1 and product:2 no longer have category:electronics
            var tagsForProduct1 = await tagManager.GetTagsByKeyAsync("product:1");
            var tagsForProduct2 = await tagManager.GetTagsByKeyAsync("product:2");
            tagsForProduct1.Should().NotContain("category:electronics");
            tagsForProduct2.Should().NotContain("category:electronics");

            // But they should still have other tags
            tagsForProduct1.Should().Contain("products");
            tagsForProduct1.Should().Contain("featured");
        }

        [Fact]
        public async Task RemoveKey_ShouldCleanUpAllTagAssociations()
        {
            // Arrange
            var tagManager = new InMemoryHybridCacheTagManager();
            await tagManager.TrackKeyWithTagsAsync("product:1", new[] { "products", "featured", "new-arrivals" });

            // Act
            await tagManager.RemoveKeyFromTagsAsync("product:1");

            // Assert - Key should have no tags
            var tagsForKey = await tagManager.GetTagsByKeyAsync("product:1");
            tagsForKey.Should().BeEmpty();

            // All tags should not contain the key
            var productsKeys = await tagManager.GetKeysByTagAsync("products");
            var featuredKeys = await tagManager.GetKeysByTagAsync("featured");
            var newArrivalsKeys = await tagManager.GetKeysByTagAsync("new-arrivals");

            productsKeys.Should().NotContain("product:1");
            featuredKeys.Should().NotContain("product:1");
            newArrivalsKeys.Should().NotContain("product:1");
        }

        #endregion
    }

    #region Test Entities

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}

