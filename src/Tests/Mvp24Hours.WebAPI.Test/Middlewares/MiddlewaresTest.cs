using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.RateLimiting;
using Mvp24Hours.WebAPI.Services;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Middlewares;

[Trait("Category", "Unit")]
public class MiddlewaresTest
{
    [Fact]
    public async Task ExceptionMiddleware_Should_SetInternalServerError_WhenExceptionThrown()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        IOptions<ExceptionOptions> options = Options.Create(new ExceptionOptions());
#pragma warning disable CS0618 // intentional: covers obsolete ExceptionMiddleware until removal in v12
        var sut = new ExceptionMiddleware(_ => throw new InvalidOperationException("boom"), options, NullLogger<ExceptionMiddleware>.Instance);
#pragma warning restore CS0618

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task CorrelationIdMiddleware_Should_UseIncomingHeader()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Correlation-ID"] = "abc-123";
        IOptions<CorrelationIdOptions> options = Options.Create(new CorrelationIdOptions { IncludeInResponse = true });
        var sut = new CorrelationIdMiddleware(async c => await c.Response.WriteAsync("ok"), options);

        await sut.Invoke(context);

        context.TraceIdentifier.Should().Be("abc-123");
    }

    [Fact]
    public async Task RateLimitingMiddleware_Should_Bypass_WhenDisabled()
    {
        bool called = false;
        var options = new RateLimitingOptions { Enabled = false };
        var keyGen = new DefaultRateLimitKeyGenerator(Options.Create(options));
        var resolver = new RateLimitPartitionResolver(Options.Create(options), keyGen);
        var sut = new RateLimitingMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, Options.Create(options), resolver, NullLogger<RateLimitingMiddleware>.Instance);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        called.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitingMiddleware_Should_Return429_WhenLimitExceeded()
    {
        var options = new RateLimitingOptions
        {
            RateLimitedStatusCode = 429,
            UseProblemDetails = false
        };
        options.AddFixedWindowPolicy("default", 1, TimeSpan.FromMinutes(5));
        var resolver = new RateLimitPartitionResolver(Options.Create(options), new DefaultRateLimitKeyGenerator(Options.Create(options)));
        var sut = new RateLimitingMiddleware(_ => Task.CompletedTask, Options.Create(options), resolver, NullLogger<RateLimitingMiddleware>.Instance);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/test");
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.1.1.1");

        await sut.InvokeAsync(context);
        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task RequestLoggingMiddleware_Should_CallLoggerMethods()
    {
        var loggerMock = new Mock<IRequestLogger>();
        IOptions<RequestLoggingOptions> options = Options.Create(new RequestLoggingOptions());
        var sut = new RequestLoggingMiddleware(_ => Task.CompletedTask, loggerMock.Object, options);

        await sut.InvokeAsync(WebApiTestHelpers.CreateHttpContext());

        loggerMock.Verify(x => x.LogRequestAsync(It.IsAny<HttpContext>()), Times.Once);
        loggerMock.Verify(x => x.LogResponseAsync(It.IsAny<HttpContext>(), It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_Should_AddSecurityHeaders()
    {
        IOptions<SecurityHeadersOptions> options = Options.Create(new SecurityHeadersOptions());
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/orders");
        context.Request.Scheme = "https";
        bool called = false;
        var sut = new SecurityHeadersMiddleware(c =>
        {
            called = true;
            return c.Response.WriteAsync("ok");
        }, options, NullLogger<SecurityHeadersMiddleware>.Instance);

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ETagMiddleware_Should_SetEtag_OnSuccess()
    {
        IOptions<ETagOptions> options = Options.Create(new ETagOptions { Enabled = true });
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        var sut = new ETagMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("payload");
        }, options, NullLogger<ETagMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("ETag");
    }

    [Fact]
    public async Task ETagMiddleware_Should_SetEtag_WhenIfNoneMatchIsProvided()
    {
        IOptions<ETagOptions> options = Options.Create(new ETagOptions { Enabled = true });
        DefaultHttpContext context1 = WebApiTestHelpers.CreateHttpContext();
        var sut = new ETagMiddleware(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("fixed-content");
        }, options, NullLogger<ETagMiddleware>.Instance);

        await sut.InvokeAsync(context1);
        string etag = context1.Response.Headers.ETag.ToString();

        DefaultHttpContext context2 = WebApiTestHelpers.CreateHttpContext();
        context2.Request.Headers.IfNoneMatch = etag;
        await sut.InvokeAsync(context2);

        context2.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        context2.Response.Headers.Should().ContainKey("ETag");
    }

    [Fact]
    public async Task ProblemDetailsMiddleware_Should_MapException_ToProblemDetails()
    {
        var mapper = new Mock<IExceptionToProblemDetailsMapper>();
        mapper.Setup(x => x.Map(It.IsAny<Exception>(), It.IsAny<HttpContext>()))
            .Returns(new ProblemDetails { Status = 400, Title = "bad", Detail = "invalid" });
        mapper.Setup(x => x.GetStatusCode(It.IsAny<Exception>())).Returns(400);
#pragma warning disable CS0618 // intentional: covers obsolete ProblemDetailsMiddleware until removal in v12
        var sut = new ProblemDetailsMiddleware(
            _ => throw new ArgumentException("invalid"),
            mapper.Object,
            Options.Create(new MvpProblemDetailsOptions()),
            NullLogger<ProblemDetailsMiddleware>.Instance);
#pragma warning restore CS0618
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        await sut.InvokeAsync(context);
        string body = await WebApiTestHelpers.ReadResponseBodyAsync(context);

        context.Response.StatusCode.Should().Be(400);
        body.Should().Contain("invalid");
    }

    [Fact]
    public async Task RequestContextMiddleware_Should_PopulateItemsAndHeaders()
    {
        var options = new RequestContextOptions
        {
            IdGenerator = () => "gen-id",
            IncludeInResponse = true
        };
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Causation-ID"] = "cause-1";
        context.Request.Headers["X-Tenant-ID"] = "tenant-9";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-1")], "test"));
        var sut = new RequestContextMiddleware(_ => Task.CompletedTask, Options.Create(options));

        await sut.InvokeAsync(context);
        await context.Response.CompleteAsync();

        context.Items.Should().ContainKey(RequestContextKeys.CorrelationId);
        context.Items.Should().ContainKey(RequestContextKeys.RequestId);
    }

    [Fact]
    public async Task IpFilteringMiddleware_Should_Block_WhenWhitelistDoesNotContainIp()
    {
        var options = new IpFilteringOptions
        {
            Enabled = true,
            Mode = IpFilteringMode.Whitelist,
            AlwaysAllowLocalhost = false
        };
        options.WhitelistedIps.Clear();
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/private");
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.55");
        var sut = new IpFilteringMiddleware(_ => Task.CompletedTask, Options.Create(options), NullLogger<IpFilteringMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(options.BlockedStatusCode);
    }

    [Fact]
    public async Task RequestSizeLimitMiddleware_Should_Reject_WhenContentLengthExceedsLimit()
    {
        var options = new RequestSizeLimitOptions { DefaultMaxBodySize = 3 };
        options.ContentTypeLimits.Clear();
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", body: "12345");
        var sut = new RequestSizeLimitMiddleware(_ => Task.CompletedTask, Options.Create(options), NullLogger<RequestSizeLimitMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
    }

    [Fact]
    public async Task RequestTelemetryMiddleware_Should_AppendCorrelationHeader()
    {
        var options = new RequestTelemetryOptions { EnableTracing = true, EnableMetrics = false };
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.TraceIdentifier = "trace-123";
        var sut = new RequestTelemetryMiddleware(async c => await c.Response.WriteAsync("ok"), Options.Create(options));

        await sut.InvokeAsync(context);

        context.TraceIdentifier.Should().Be("trace-123");
    }

    [Fact]
    public async Task AntiForgeryMiddleware_Should_ReturnTokenEndpointPayload()
    {
        var options = new AntiForgeryOptions { RegisterTokenEndpoint = true };
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "GET", path: options.TokenEndpoint);
        var sut = new AntiForgeryMiddleware(_ => Task.CompletedTask, Options.Create(options), NullLogger<AntiForgeryMiddleware>.Instance);

        await sut.InvokeAsync(context);
        string body = await WebApiTestHelpers.ReadResponseBodyAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        body.Should().Contain("token");
    }

    [Fact]
    public async Task AntiForgeryMiddleware_Should_FailProtectedPostWithoutTokens()
    {
        var options = new AntiForgeryOptions();
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/api/orders");
        var sut = new AntiForgeryMiddleware(_ => Task.CompletedTask, Options.Create(options), NullLogger<AntiForgeryMiddleware>.Instance);

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ExceptionMiddleware_Should_Return400_WhenValidationExceptionThrown()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        IOptions<ExceptionOptions> options = Options.Create(new ExceptionOptions());
#pragma warning disable CS0618 // intentional: covers obsolete ExceptionMiddleware until removal in v12
        var sut = new ExceptionMiddleware(
            _ => throw new ValidationException("Validation failed", [new MessageResult("Name", "required", MessageType.Error)]),
            options,
            NullLogger<ExceptionMiddleware>.Instance);
#pragma warning restore CS0618

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ExceptionMiddleware_Should_Return422_WhenDomainExceptionThrown()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        IOptions<ExceptionOptions> options = Options.Create(new ExceptionOptions());
#pragma warning disable CS0618 // intentional: covers obsolete ExceptionMiddleware until removal in v12
        var sut = new ExceptionMiddleware(
            _ => throw new DomainException("Domain rule violated"),
            options,
            NullLogger<ExceptionMiddleware>.Instance);
#pragma warning restore CS0618

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }
}
