using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Integration.Caching;

namespace Mvp24Hours.Application.Pipe.Test.Integration.Caching;

[Trait("Category", "Unit")]
public class CacheResultsMiddlewareTest
{
    private static MemoryDistributedCache CreateCache()
    {
        return new(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrow()
    {
        Action act = () => new CacheResultsMiddleware(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public async Task ExecuteAsync_WithoutToken_ShouldInvokeNext()
    {
        MemoryDistributedCache cache = CreateCache();
        var middleware = new CacheResultsMiddleware(cache, NullLogger<CacheResultsMiddleware>.Instance);
        var message = new TokenlessMessage();
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheMiss_ShouldInvokeNextAndCacheResult()
    {
        MemoryDistributedCache cache = CreateCache();
        var options = new CacheOperationOptions { CacheKeyPrefix = "test:" };
        var middleware = new CacheResultsMiddleware(cache, NullLogger<CacheResultsMiddleware>.Instance, options);
        var message = new PipelineMessage("token-1");
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        string? cached = await cache.GetStringAsync("test:msg:token-1");
        cached.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_OnCacheHit_ShouldSkipNext()
    {
        MemoryDistributedCache cache = CreateCache();
        var options = new CacheOperationOptions { CacheKeyPrefix = "hit:" };
        var middleware = new CacheResultsMiddleware(cache, NullLogger<CacheResultsMiddleware>.Instance, options);
        var message = new PipelineMessage("token-hit");

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        bool nextCalled = false;
        await middleware.ExecuteAsync(new PipelineMessage("token-hit"), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheReadFails_ShouldStillInvokeNext()
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("read failed"));
        var middleware = new CacheResultsMiddleware(cacheMock.Object, NullLogger<CacheResultsMiddleware>.Instance);
        var message = new PipelineMessage("token-read-fail");
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCacheWriteFails_ShouldCompletePipeline()
    {
        var cacheMock = new Mock<IDistributedCache>();
        cacheMock
            .Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        cacheMock
            .Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("write failed"));
        var middleware = new CacheResultsMiddleware(cacheMock.Object, NullLogger<CacheResultsMiddleware>.Instance);
        var message = new PipelineMessage("token-write-fail");
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockedAndCacheFailedResultsDisabled_ShouldNotCache()
    {
        MemoryDistributedCache cache = CreateCache();
        var options = new CacheOperationOptions { CacheFailedResults = false, CacheKeyPrefix = "lock:" };
        var middleware = new CacheResultsMiddleware(cache, NullLogger<CacheResultsMiddleware>.Instance, options);
        var message = new PipelineMessage("token-locked");
        message.SetLock();

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        string? cached = await cache.GetStringAsync("lock:msg:token-locked");
        cached.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockedAndCacheFailedResultsEnabled_ShouldCache()
    {
        MemoryDistributedCache cache = CreateCache();
        var options = new CacheOperationOptions { CacheFailedResults = true, CacheKeyPrefix = "fail:" };
        var middleware = new CacheResultsMiddleware(cache, NullLogger<CacheResultsMiddleware>.Instance, options);
        var message = new PipelineMessage("token-fail-cache");
        message.SetLock();

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        string? cached = await cache.GetStringAsync("fail:msg:token-fail-cache");
        cached.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCachedData_ShouldInvokeNext()
    {
        MemoryDistributedCache cache = CreateCache();
        var options = new CacheOperationOptions { CacheKeyPrefix = "bad:" };
        await cache.SetStringAsync("bad:msg:token-bad", "{not-json", new DistributedCacheEntryOptions());
        var middleware = new CacheResultsMiddleware(cache, NullLogger<CacheResultsMiddleware>.Instance, options);
        bool nextCalled = false;

        await middleware.ExecuteAsync(new PipelineMessage("token-bad"), () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void CacheResultAttribute_GenerateKey_WithToken_ShouldReturnToken()
    {
        var attribute = new CacheResultAttribute { UseTokenAsKey = true };
        IPipelineMessage message = new PipelineMessage("my-token");

        string? key = attribute.GenerateKey(message);

        key.Should().Be("my-token");
    }

    [Fact]
    public void CacheResultAttribute_GenerateKey_WithKeyPattern_ShouldReturnPattern()
    {
        var attribute = new CacheResultAttribute { UseTokenAsKey = false, KeyPattern = "pattern-{id}" };
        IPipelineMessage message = new PipelineMessage("ignored");

        string? key = attribute.GenerateKey(message);

        key.Should().Be("pattern-{id}");
    }

    [Fact]
    public void Order_ShouldBe100()
    {
        var middleware = new CacheResultsMiddleware(CreateCache());

        middleware.Order.Should().Be(100);
    }

    private sealed class TokenlessMessage : IPipelineMessage
    {
        public bool IsFaulty { get; private set; }
        public IList<IMessageResult> Messages { get; } = [];
        public string Token => null!;
        public bool IsLocked { get; private set; }
        public dynamic DynamicContents => null!;

        public void AddContent<T>(T obj) { }
        public void AddContent<T>(string key, T obj) { }
        public T GetContent<T>()
        {
            return default!;
        }

        public T GetContent<T>(string key)
        {
            return default!;
        }

        public bool HasContent<T>()
        {
            return false;
        }

        public bool HasContent(string key)
        {
            return false;
        }

        public IList<object> GetContentAll()
        {
            return [];
        }

        public void SetLock()
        {
            IsLocked = true;
        }

        public void SetFailure()
        {
            IsFaulty = true;
        }
    }
}
