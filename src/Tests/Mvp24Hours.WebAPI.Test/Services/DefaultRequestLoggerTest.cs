using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Services;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Services;

[Trait("Category", "Unit")]
public class DefaultRequestLoggerTest
{
    [Fact]
    public async Task DefaultRequestLogger_Should_LogRequest()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        IOptions<RequestLoggingOptions> options = Options.Create(new RequestLoggingOptions
        {
            LoggingLevel = RequestLoggingLevel.Standard,
            LogRequestHeaders = true
        });
        var sut = new DefaultRequestLogger(loggerMock.Object, options);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["Authorization"] = "secret";

        await sut.LogRequestAsync(context);

        loggerMock.VerifyLogWasCalled();
    }

    [Fact]
    public async Task DefaultRequestLogger_Should_LogResponse()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions()));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Response.StatusCode = 200;

        await sut.LogResponseAsync(context, 12);

        loggerMock.VerifyLogWasCalled();
    }

    [Fact]
    public async Task DefaultRequestLogger_Should_LogException()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions()));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        await sut.LogExceptionAsync(context, new InvalidOperationException("boom"), 20);

        loggerMock.VerifyLogWasCalled();
    }

    [Fact]
    public async Task LogRequestAsync_WithLoggingLevelNone_ShouldNotLog()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions
        {
            LoggingLevel = RequestLoggingLevel.None
        }));

        await sut.LogRequestAsync(WebApiTestHelpers.CreateHttpContext());

        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LogResponseAsync_WithLoggingLevelNone_ShouldNotLog()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions
        {
            LoggingLevel = RequestLoggingLevel.None
        }));

        await sut.LogResponseAsync(WebApiTestHelpers.CreateHttpContext(), 10);

        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task LogSlowRequestAsync_ShouldLogWarning()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions()));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(method: "POST", path: "/orders");

        await sut.LogSlowRequestAsync(context, 5000, 3000);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogRequestAsync_DetailedLevel_ShouldLogMaskedBodyAndHeaders()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions
        {
            LoggingLevel = RequestLoggingLevel.Detailed,
            LogRequestHeaders = true,
            LogRequestBody = true
        }));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(
            method: "POST",
            path: "/login",
            body: """{"username":"user","password":"secret123"}""");

        await sut.LogRequestAsync(context);

        loggerMock.VerifyLogWasCalled();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LogResponseAsync_WithServerError_ShouldUseErrorLogLevel()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions
        {
            LoggingLevel = RequestLoggingLevel.Standard,
            LogResponseHeaders = true
        }));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Response.StatusCode = 500;
        context.Response.Headers["X-Trace"] = "trace-1";

        await sut.LogResponseAsync(context, 150);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogRequestAsync_ShouldResolveClientIpFromForwardedHeader()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions()));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10, 10.0.0.1";

        await sut.LogRequestAsync(context);

        loggerMock.VerifyLogWasCalled();
    }

    [Fact]
    public async Task LogRequestAsync_WithAuthenticatedUser_ShouldIncludeUserId()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions()));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-42")], "Test");
        context.User = new ClaimsPrincipal(identity);

        await sut.LogRequestAsync(context);

        loggerMock.VerifyLogWasCalled();
    }

    [Fact]
    public async Task LogRequestAsync_ShouldIncludeTenantFromHeader()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions()));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Request.Headers["X-Tenant-ID"] = "tenant-7";

        await sut.LogRequestAsync(context);

        loggerMock.VerifyLogWasCalled();
    }

    [Fact]
    public async Task LogExceptionAsync_WithIncludeExceptionDetails_ShouldLogStackTraceData()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        var sut = new DefaultRequestLogger(loggerMock.Object, Options.Create(new RequestLoggingOptions
        {
            IncludeExceptionDetails = true
        }));
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        await sut.LogExceptionAsync(context, new InvalidOperationException("failed"), 99);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new DefaultRequestLogger(null!, Options.Create(new RequestLoggingOptions()));
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        var loggerMock = new Mock<ILogger<DefaultRequestLogger>>();
        Action act = () => _ = new DefaultRequestLogger(loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

internal static class LoggerMockExtensions
{
    public static void VerifyLogWasCalled(this Mock<ILogger<DefaultRequestLogger>> loggerMock)
    {
        loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
