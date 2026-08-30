using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class AntiForgeryMiddlewareTest
{
    private static AntiForgeryMiddleware CreateSut(
        RequestDelegate next,
        AntiForgeryOptions? options = null)
    {
        return new AntiForgeryMiddleware(
            next,
            Options.Create(options ?? new AntiForgeryOptions()),
            NullLogger<AntiForgeryMiddleware>.Instance);
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNextWithoutValidation()
    {
        // Arrange
        bool called = false;
        var options = new AntiForgeryOptions { Enabled = false };
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenTokensMatch_CallsNext()
    {
        // Arrange
        bool called = false;
        var options = new AntiForgeryOptions();
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
        context.Request.Headers.Append("Cookie", $"{options.CookieName}=matching-token");
        context.Request.Headers[options.HeaderName] = "matching-token";

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokensDoNotMatch_ReturnsBadRequest()
    {
        // Arrange
        var sut = CreateSut(_ => Task.CompletedTask);
        var options = new AntiForgeryOptions();
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
        context.Request.Headers.Append("Cookie", $"{options.CookieName}=cookie-token");
        context.Request.Headers[options.HeaderName] = "different-token";
        var sut2 = CreateSut(_ => Task.CompletedTask, options);

        // Act
        await sut2.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderMissing_ReturnsBadRequest()
    {
        // Arrange
        var options = new AntiForgeryOptions();
        var sut = CreateSut(_ => Task.CompletedTask, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
        context.Request.Headers.Append("Cookie", $"{options.CookieName}=cookie-token");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        string body = await WebApiTestHelpers.ReadResponseBodyAsync(context);
        body.Should().Contain("Anti-forgery token validation failed");
    }

    [Fact]
    public async Task InvokeAsync_WhenSkipValidationForRequestsWithoutCookies_BypassesValidation()
    {
        // Arrange
        bool called = false;
        var options = new AntiForgeryOptions { SkipValidationForRequestsWithoutCookies = true };
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenPathIsExcluded_BypassesValidationAndCallsNext()
    {
        // Arrange
        bool called = false;
        var options = new AntiForgeryOptions();
        options.ExcludedPaths.Add("/api/auth/login");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/auth/login");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenMethodIsNotProtected_BypassesValidationAndCallsNext()
    {
        // Arrange
        bool called = false;
        var options = new AntiForgeryOptions();
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "GET", path: "/api/orders");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenProtectedPathsConfigured_OnlyMatchingPathIsValidated()
    {
        // Arrange
        bool called = false;
        var options = new AntiForgeryOptions();
        options.ProtectedPaths.Add("/api/admin/*");
        var sut = CreateSut(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");

        // Act - path does not match any ProtectedPaths pattern, so RequiresValidation returns false
        await sut.InvokeAsync(context);

        // Assert
        called.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenRefreshTokenOnEachRequest_SetsNewCookieAfterNext()
    {
        // Arrange
        var options = new AntiForgeryOptions { RefreshTokenOnEachRequest = true };
        var sut = CreateSut(_ => Task.CompletedTask, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
        context.Request.Headers.Append("Cookie", $"{options.CookieName}=matching-token");
        context.Request.Headers[options.HeaderName] = "matching-token";

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task InvokeAsync_TokenEndpoint_ReturnsTokenAsJson()
    {
        // Arrange
        var options = new AntiForgeryOptions { RegisterTokenEndpoint = true, TokenEndpoint = "/api/csrf-token" };
        var sut = CreateSut(_ => Task.CompletedTask, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "GET", path: "/api/csrf-token");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        string body = await WebApiTestHelpers.ReadResponseBodyAsync(context);
        body.Should().Contain("token");
        context.Response.Headers.Should().ContainKey("Set-Cookie");
    }

    [Fact]
    public async Task InvokeAsync_GetRequestWithoutExistingCookie_SetsTokenCookie()
    {
        // Arrange
        var options = new AntiForgeryOptions();
        var sut = CreateSut(_ => Task.CompletedTask, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "GET", path: "/api/orders");

        // Act
        await sut.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Set-Cookie");
    }
}
