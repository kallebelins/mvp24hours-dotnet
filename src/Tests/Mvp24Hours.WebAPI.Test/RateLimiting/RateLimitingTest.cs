using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
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
        IOptions<RateLimitingOptions> options = Options.Create(new RateLimitingOptions());
        var policy = new RateLimitPolicy { Name = "p1", KeySource = RateLimitKeySource.None };
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        var sut = new DefaultRateLimitKeyGenerator(options);

        string key = sut.GenerateKey(context, policy);

        key.Should().Be("p1:global");
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_UseIpAndUser()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.8.0.1");
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"));
        var policy = new RateLimitPolicy { Name = "api", KeySource = RateLimitKeySource.ClientIp | RateLimitKeySource.UserId };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(new RateLimitingOptions()));

        string key = sut.GenerateKey(context, policy);

        key.Should().Contain("ip:10.8.0.1").And.Contain("user:user-1");
    }

    [Fact]
    public void RateLimitPartitionResolver_Should_ReturnNoLimiter_WhenDisabled()
    {
        var options = new RateLimitingOptions { Enabled = false };
        var sut = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));

        RateLimitPartition<string> partition = sut.GetPartition(WebApiTestHelpers.CreateHttpContext());

        partition.Should().NotBeNull();
    }

    [Fact]
    public void RateLimitPartitionResolver_Should_ResolveDefaultPolicy()
    {
        var options = new RateLimitingOptions();
        options.AddFixedWindowPolicy("default", 10, TimeSpan.FromMinutes(1));
        var sut = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/orders");
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.10");

        RateLimitPartition<string> partition = sut.GetPartition(context);

        partition.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemoryRateLimiter_Should_TrackCount()
    {
        var sut = new InMemoryRateLimiter(NullLogger<InMemoryRateLimiter>.Instance);

        DistributedRateLimitResult result = await sut.TryAcquireAsync("k1", 2, TimeSpan.FromMinutes(1));
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
        IDistributedCache cache = WebApiTestHelpers.CreateMemoryDistributedCache();
        IOptions<DistributedRateLimitingOptions> options = Options.Create(new DistributedRateLimitingOptions { InstanceName = "test:" });
        var sut = new RedisDistributedRateLimiter(cache, options, NullLogger<RedisDistributedRateLimiter>.Instance);

        DistributedRateLimitResult result = await sut.TryAcquireAsync("key-1", 5, TimeSpan.FromMinutes(1));
        long count = await sut.GetCurrentCountAsync("key-1");

        result.IsAcquired.Should().BeTrue();
        count.Should().Be(1);
    }

    [Fact]
    public async Task RedisDistributedRateLimiter_Should_ResetAsync()
    {
        IDistributedCache cache = WebApiTestHelpers.CreateMemoryDistributedCache();
        IOptions<DistributedRateLimitingOptions> options = Options.Create(new DistributedRateLimitingOptions { InstanceName = "test:" });
        var sut = new RedisDistributedRateLimiter(cache, options, NullLogger<RedisDistributedRateLimiter>.Instance);
        await sut.TryAcquireAsync("key-1", 5, TimeSpan.FromMinutes(1));

        await sut.ResetAsync("key-1");
        long count = await sut.GetCurrentCountAsync("key-1");

        count.Should().Be(0);
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_UseApiKeyFromHeader()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Api-Key"] = "secret-key";
        var policy = new RateLimitPolicy { Name = "api", KeySource = RateLimitKeySource.ApiKey };
        var options = new RateLimitingOptions { ApiKeyHeaderName = "X-Api-Key" };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(options));

        string key = sut.GenerateKey(context, policy);

        key.Should().Contain("apikey:secret-key");
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_UseTenantIdFromHeader()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "tenant-42";
        var policy = new RateLimitPolicy { Name = "api", KeySource = RateLimitKeySource.TenantId };
        var options = new RateLimitingOptions { TenantIdHeaderName = "X-Tenant-ID" };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(options));

        string key = sut.GenerateKey(context, policy);

        key.Should().Contain("tenant:tenant-42");
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_UseCustomHeader()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Partner-ID"] = "partner-7";
        var policy = new RateLimitPolicy
        {
            Name = "api",
            KeySource = RateLimitKeySource.CustomHeader,
            CustomHeaderName = "X-Partner-ID"
        };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(new RateLimitingOptions()));

        string key = sut.GenerateKey(context, policy);

        key.Should().Contain("header:partner-7");
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_UseForwardedHeaderWhenEnabled()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10, 10.8.0.1";
        var options = new RateLimitingOptions { UseForwardedHeaders = true };
        var policy = new RateLimitPolicy { Name = "api", KeySource = RateLimitKeySource.ClientIp };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(options));

        string key = sut.GenerateKey(context, policy);

        key.Should().Contain("ip:203.0.113.10");
    }

    [Fact]
    public void DefaultRateLimitKeyGenerator_Should_FallbackToIpWhenNoKeyPartsMatch()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.20");
        var policy = new RateLimitPolicy { Name = "api", KeySource = RateLimitKeySource.UserId };
        var sut = new DefaultRateLimitKeyGenerator(Options.Create(new RateLimitingOptions()));

        string key = sut.GenerateKey(context, policy);

        key.Should().Be("api:ip:198.51.100.20");
    }

    [Fact]
    public void RateLimitPartitionResolver_Should_BypassWhitelistedIp()
    {
        var options = new RateLimitingOptions();
        options.WhitelistedIps.Add("203.0.113.50");
        options.AddFixedWindowPolicy("default", 1, TimeSpan.FromMinutes(1));
        var sut = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/orders");
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.50");

        RateLimitPartition<string> partition = sut.GetPartition(context);

        partition.PartitionKey.Should().Be("bypassed");
    }

    [Fact]
    public void RateLimitPartitionResolver_Should_UseEndpointSpecificPolicy()
    {
        var options = new RateLimitingOptions();
        options.AddFixedWindowPolicy("orders", 5, TimeSpan.FromMinutes(1));
        options.EndpointPolicies["/api/orders/**"] = "orders";
        var sut = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/orders/123");
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.8.0.5");

        RateLimitPartition<string> partition = sut.GetPartition(context);

        partition.PartitionKey.Should().StartWith("orders:");
    }
}
