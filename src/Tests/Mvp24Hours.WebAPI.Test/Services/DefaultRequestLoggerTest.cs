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
