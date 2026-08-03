using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Core.Observability;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class StructuredLoggingCoreExtensionsTest
{
    private readonly Mock<ILogger> _logger = new();

    public StructuredLoggingCoreExtensionsTest()
    {
        _logger
            .Setup(l => l.BeginScope(It.IsAny<object>()))
            .Returns(Mock.Of<IDisposable>());
        _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public void LogInformationWithTrace_ShouldLogInformation()
    {
        _logger.Object.LogInformationWithTrace("hello {Name}", "world");

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWarningWithTrace_ShouldLogWarning()
    {
        _logger.Object.LogWarningWithTrace("warn {Code}", 42);

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogErrorWithTrace_ShouldLogErrorWithException()
    {
        var exception = new InvalidOperationException("boom");

        _logger.Object.LogErrorWithTrace(exception, "failed {Id}", 1);

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogCriticalWithTrace_ShouldLogCriticalWithException()
    {
        var exception = new InvalidOperationException("critical");

        _logger.Object.LogCriticalWithTrace(exception, "critical {Id}", 2);

        _logger.Verify(
            l => l.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogHttpRequest_ShouldLogStructuredHttpMessage()
    {
        _logger.Object.LogHttpRequest("GET", "/api/orders", 200, 15);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDatabaseOperation_WithRowsAffected_ShouldLogInformation()
    {
        _logger.Object.LogDatabaseOperation("mongodb", "UPDATE", "orders", 12, 3);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDatabaseOperation_WithoutRowsAffected_ShouldLogInformation()
    {
        _logger.Object.LogDatabaseOperation("postgresql", "SELECT", "customers", 8);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogMessagingOperation_ShouldLogInformation()
    {
        _logger.Object.LogMessagingOperation("rabbitmq", "orders.created", "publish", "msg-1");

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogMediatorRequest_OnSuccess_ShouldLogInformation()
    {
        _logger.Object.LogMediatorRequest("CreateOrderCommand", "Command", 25, success: true);

        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogMediatorRequest_OnFailure_ShouldLogWarning()
    {
        _logger.Object.LogMediatorRequest("CreateOrderCommand", "Command", 25, success: false);

        _logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((_, _) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
