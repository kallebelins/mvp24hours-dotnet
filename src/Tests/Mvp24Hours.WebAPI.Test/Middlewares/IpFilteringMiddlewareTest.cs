using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class IpFilteringMiddlewareTest
{
    private static IpFilteringMiddleware CreateSut(RequestDelegate next, IpFilteringOptions options)
    {
        return new IpFilteringMiddleware(next, Options.Create(options), NullLogger<IpFilteringMiddleware>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNext()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = false };
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ModeDisabled_CallsNext()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Disabled };
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhitelistMode_IpInWhitelist_CallsNext()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = false };
        options.WhitelistedIps.Clear();
        options.WhitelistedIps.Add("203.0.113.10");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_BlacklistMode_IpInBlacklist_Blocks()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Blacklist, AlwaysAllowLocalhost = false };
        options.BlacklistedIps.Add("198.51.100.5");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.5");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeFalse();
        context.Response.StatusCode.Should().Be(options.BlockedStatusCode);
    }

    [Fact]
    public async Task InvokeAsync_BlacklistMode_IpNotInBlacklist_CallsNext()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Blacklist, AlwaysAllowLocalhost = false };
        options.BlacklistedIps.Add("198.51.100.5");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.99");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_CidrRange_MatchingIp_IsAllowedInWhitelist()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = false };
        options.WhitelistedIps.Clear();
        options.WhitelistedIps.Add("10.8.0.0/16");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.8.5.20");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_CidrRange_NonMatchingIp_IsBlockedInWhitelist()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = false };
        options.WhitelistedIps.Clear();
        options.WhitelistedIps.Add("10.8.0.0/16");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.5");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeFalse();
        context.Response.StatusCode.Should().Be(options.BlockedStatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysAllowLocalhost_BypassesFiltering()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = true };
        options.WhitelistedIps.Clear();
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = IPAddress.Loopback;

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ExcludedPath_BypassesFiltering()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = false };
        options.WhitelistedIps.Clear();
        options.ExcludedPaths.Add("/health");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/health");
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_MissingRemoteIpAddress_BlocksAsUnknown()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = false };
        options.WhitelistedIps.Clear();
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Connection.RemoteIpAddress = null;

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeFalse();
        context.Response.StatusCode.Should().Be(options.BlockedStatusCode);
    }

    [Fact]
    public async Task InvokeAsync_PathSpecificWhitelist_OverridesGlobalRule()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Blacklist, AlwaysAllowLocalhost = false };
        options.PathWhitelists["/api/admin/*"] = ["10.0.0.5"];
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/admin/settings");
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        await sut.InvokeAsync(context);

        // Assert - even though mode is Blacklist (and IP isn't blacklisted so it'd normally pass),
        // the path-specific whitelist rule takes precedence and explicitly allows this IP.
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_PathSpecificBlacklist_BlocksMatchingIp()
    {
        // Arrange
        bool called = false;
        var options = new IpFilteringOptions { Enabled = true, Mode = IpFilteringMode.Whitelist, AlwaysAllowLocalhost = false };
        options.WhitelistedIps.Clear();
        options.WhitelistedIps.Add("10.0.0.5");
        options.PathBlacklists["/api/admin/*"] = ["10.0.0.5"];
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/admin/settings");
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.5");

        // Act
        await sut.InvokeAsync(context);

        // Assert - path-specific blacklist blocks this IP even though it's globally whitelisted.
        called.Should().BeFalse();
        context.Response.StatusCode.Should().Be(options.BlockedStatusCode);
    }
}
