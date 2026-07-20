//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Mvp24Hours.Infrastructure.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Extensions;

[Trait("Category", "Unit")]
public class HttpContextExtensionsTest
{
    [Fact]
    public void GetUserIP_WithRemoteAddress_ShouldReturnRemoteIp()
    {
        DefaultHttpContext context = CreateContext(IPAddress.Parse("203.0.113.50"));

        context.GetUserIP().Should().Be("203.0.113.50");
    }

    [Fact]
    public void GetUserIP_WithIpv6Address_ShouldReturnFirstSegment()
    {
        DefaultHttpContext context = CreateContext(IPAddress.Parse("2001:db8::1"));

        context.GetUserIP().Should().Be("2001");
    }

    [Fact]
    public void GetUserIP_WithNullContext_ShouldReturnDefaultIp()
    {
#pragma warning disable CS8600, CS8604 // Intentional null extension receiver
        HttpContext context = null;
        context.GetUserIP().Should().Be("0.0.0.0");
#pragma warning restore CS8600, CS8604
    }

    [Fact]
    public void GetUserIP_WithAccessor_ShouldReturnRemoteIp()
    {
        var accessor = new HttpContextAccessor
        {
            HttpContext = CreateContext(IPAddress.Parse("198.51.100.10"))
        };

        accessor.GetUserIP().Should().Be("198.51.100.10");
    }

    [Fact]
    public void GetUserIP_WithNullAccessor_ShouldReturnNull()
    {
#pragma warning disable CS8600, CS8604 // Intentional null extension receiver
        IHttpContextAccessor accessor = null;
        accessor.GetUserIP().Should().BeNull();
#pragma warning restore CS8600, CS8604
    }

    [Fact]
    public void GetBaseUrl_WithHttpContext_ShouldReturnSchemeHostAndPathBase()
    {
        DefaultHttpContext context = CreateContext(IPAddress.Loopback);
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("api.example.com");
        context.Request.PathBase = new PathString("/v1");

        context.GetBaseUrl().Should().Be("https://api.example.com/v1");
    }

    [Fact]
    public void GetBaseUrl_WithAccessor_ShouldReturnBaseUrl()
    {
        DefaultHttpContext context = CreateContext(IPAddress.Loopback);
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost:5000");
        context.Request.PathBase = PathString.Empty;

        var accessor = new HttpContextAccessor { HttpContext = context };

        accessor.GetBaseUrl().Should().Be("http://localhost:5000");
    }

    [Fact]
    public void GetBaseUrl_WithNullContext_ShouldReturnNull()
    {
#pragma warning disable CS8600, CS8604 // Intentional null extension receiver
        HttpContext context = null;
        context.GetBaseUrl().Should().BeNull();
#pragma warning restore CS8600, CS8604
    }

    private static DefaultHttpContext CreateContext(IPAddress remoteIpAddress)
    {
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpConnectionFeature>(new TestHttpConnectionFeature
        {
            RemoteIpAddress = remoteIpAddress
        });
        return context;
    }

    private sealed class TestHttpConnectionFeature : IHttpConnectionFeature
    {
        public string ConnectionId { get; set; } = string.Empty;

        public IPAddress? LocalIpAddress { get; set; }

        public int LocalPort { get; set; }

        public IPAddress? RemoteIpAddress { get; set; }

        public int RemotePort { get; set; }
    }
}
