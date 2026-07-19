using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.RateLimiting;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.RateLimiting;

[Trait("Category", "Unit")]
public class RateLimitingTest
{
    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_CreateGlobalKey_WhenSourceNone()
    {
        var options = Options.Create(new RateLimitingOptions());
        var policy = new RateLimitPolicy { Name = "p1", KeySource = RateLimitKeySource.None };
        var context = WebApiTestHelpers.CreateHttpContext();
        var sut = new DefaultRateLimitKeyGenerator(options);

        string key = sut.GenerateKey(context, policy);

        key.Should().Be("p1:global");
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_UseIpAndUser()
    {
        var context = WebApiTestHelpers.CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"));
        var policy = new RateLimitPolicy { Name = "api", KeySource = RateLimitKeySource.ClientIp | RateLimitKeySource.UserId };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(new RateLimitingOptions()));

        string key = sut.GenerateKey(context, policy);

        key.Should().Contain("ip:10.0.0.1").And.Contain("user:user-1");
    }

    [Fact]
    public void RateLimitPartitionResolver_Should_ReturnNoLimiter_WhenDisabled()
    {
        var options = new RateLimitingOptions { Enabled = false };
        var sut = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));

        var partition = sut.GetPartition(WebApiTestHelpers.CreateHttpContext());

        partition.Should().NotBeNull();
    }

    [Fact]
    public void RateLimitPartitionResolver_Should_ResolveDefaultPolicy()
    {
        var options = new RateLimitingOptions();
        options.AddFixedWindowPolicy("default", 10, TimeSpan.FromMinutes(1));
        var sut = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));
        var context = WebApiTestHelpers.CreateHttpContext(path: "/api/orders");
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");

        var partition = sut.GetPartition(context);

        partition.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemoryRateLimiter_Should_TrackCount()
    {
        var sut = new InMemoryRateLimiter(NullLogger<InMemoryRateLimiter>.Instance);

        var result = await sut.TryAcquireAsync("k1", 2, TimeSpan.FromMinutes(1));
        long count = await sut.GetCurrentCountAsync("k1");

        result.IsAcquired.Should().BeTrue();
        count.Should().Be(1);
    }

    [Fact]
    public async Task InMemoryRateLimiter_Should_ResetCount()
    {
        var sut = new InMemoryRateLimiter(NullLogger<InMemoryRateLimiter>.Instance);
        await sut.TryAcquireAsync("k1", 2, TimeSpan.FromMinutes(1));

        await sut.ResetAsync("k1");
        long count = await sut.GetCurrentCountAsync("k1");

        count.Should().Be(0);
    }

    [Fact]
    public async Task RedisDistributedRateLimiter_Should_StoreStateInDistributedCache()
    {
        var cache = WebApiTestHelpers.CreateMemoryDistributedCache();
        var options = Options.Create(new DistributedRateLimitingOptions { InstanceName = "test:" });
        var sut = new RedisDistributedRateLimiter(cache, options, NullLogger<RedisDistributedRateLimiter>.Instance);

        var result = await sut.TryAcquireAsync("key-1", 5, TimeSpan.FromMinutes(1));
        long count = await sut.GetCurrentCountAsync("key-1");

        result.IsAcquired.Should().BeTrue();
        count.Should().Be(1);
    }

    [Fact]
    public async Task RedisDistributedRateLimiter_Should_ResetAsync()
    {
        var cache = WebApiTestHelpers.CreateMemoryDistributedCache();
        var options = Options.Create(new DistributedRateLimitingOptions { InstanceName = "test:" });
        var sut = new RedisDistributedRateLimiter(cache, options, NullLogger<RedisDistributedRateLimiter>.Instance);
        await sut.TryAcquireAsync("key-1", 5, TimeSpan.FromMinutes(1));

        await sut.ResetAsync("key-1");
        long count = await sut.GetCurrentCountAsync("key-1");

        count.Should().Be(0);
    }
}
