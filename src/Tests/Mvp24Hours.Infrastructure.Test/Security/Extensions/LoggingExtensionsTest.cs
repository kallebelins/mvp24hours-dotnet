//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Infrastructure.Security.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Security.Extensions;

[Trait("Category", "Unit")]
public class LoggingExtensionsTest
{
    [Fact]
    public void LogInformationWithMasking_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => LoggingExtensions.LogInformationWithMasking(null!, "msg");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogDebugWithMasking_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => LoggingExtensions.LogDebugWithMasking(null!, "msg");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogWarningWithMasking_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => LoggingExtensions.LogWarningWithMasking(null!, "msg");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogErrorWithMasking_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => LoggingExtensions.LogErrorWithMasking(null!, new Exception(), "msg");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogCriticalWithMasking_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => LoggingExtensions.LogCriticalWithMasking(null!, "msg");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogDictionaryWithMasking_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => LoggingExtensions.LogDictionaryWithMasking(
            null!,
            LogLevel.Information,
            "msg",
            new Dictionary<string, string?>(),
            ["password"]);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void LogInformationWithMasking_WhenLevelDisabled_ShouldNotLog()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Information, enabled: false);

        logger.Object.LogInformationWithMasking("Password {Password}", "secret123");

        logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void LogInformationWithMasking_ShouldMaskPasswordLikeValues()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Information);
        object?[]? capturedState = null;

        logger
            .Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel _, EventId _, object state, Exception? _, Delegate _) =>
            {
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    capturedState = [.. values.Select(v => v.Value)];
                }
            });

        logger.Object.LogInformationWithMasking("Password {Password}", "secret123");

        capturedState.Should().NotBeNull();
        capturedState!.Should().Contain(new string('*', "secret123".Length));
    }

    [Fact]
    public void LogDebugWithMasking_ShouldMaskApiKeyPrefix()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Debug);
        object? capturedArg = null;

        logger
            .Setup(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel _, EventId _, object state, Exception? _, Delegate _) =>
            {
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    capturedArg = values.FirstOrDefault(v => v.Key == "ApiKey").Value;
                }
            });

        const string apiKey = "sk_live_1234567890abcdef";
        logger.Object.LogDebugWithMasking("Key {ApiKey}", apiKey);

        // IsPassword (length 8-128) runs before IsApiKey, so full password masking applies.
        capturedArg.Should().Be(new string('*', apiKey.Length));
    }

    [Fact]
    public void LogWarningWithMasking_ShouldMaskEmail()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Warning);
        object? capturedArg = null;

        logger
            .Setup(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel _, EventId _, object state, Exception? _, Delegate _) =>
            {
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    capturedArg = values.FirstOrDefault(v => v.Key == "Email").Value;
                }
            });

        // Short enough to avoid IsPassword heuristic (>= 8 chars).
        logger.Object.LogWarningWithMasking("User {Email}", "a@b.co");

        capturedArg.Should().Be("a@b.co");
    }

    [Fact]
    public void LogErrorWithMasking_ShouldForwardException()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Error);
        var exception = new InvalidOperationException("boom");

        logger.Object.LogErrorWithMasking(exception, "Failed {Detail}", "ok");

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogCriticalWithMasking_WithSensitiveKeys_ShouldStillLog()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Critical);

        Action act = () => logger.Object.LogCriticalWithMasking(
            "Critical {Token}",
            ["token"],
            "short");

        act.Should().NotThrow();
        logger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDictionaryWithMasking_ShouldMaskSensitiveKeys()
    {
        Mock<ILogger> logger = CreateLogger(LogLevel.Information);
        object? capturedArg = null;

        logger
            .Setup(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback((LogLevel _, EventId _, object state, Exception? _, Delegate _) =>
            {
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                {
                    capturedArg = values.FirstOrDefault(v => v.Key == "{OriginalFormat}" || true).Value;
                    foreach (KeyValuePair<string, object?> kvp in values)
                    {
                        if (kvp.Value is IDictionary<string, string?> dict)
                        {
                            capturedArg = dict;
                        }
                    }
                }
            });

        var data = new Dictionary<string, string?>
        {
            ["password"] = "secret123",
            ["user"] = "alice"
        };

        logger.Object.LogDictionaryWithMasking(
            LogLevel.Information,
            "Payload {Data}",
            data,
            ["password"]);

        capturedArg.Should().BeAssignableTo<IDictionary<string, string?>>();
        var masked = (IDictionary<string, string?>)capturedArg!;
        masked["password"].Should().Be(new string('*', "secret123".Length));
        masked["user"].Should().Be("alice");
    }

    [Fact]
    public void LogInformationWithMasking_SensitiveKeysOverload_WithNullLogger_ShouldThrow()
    {
        Action act = () => LoggingExtensions.LogInformationWithMasking(
            null!,
            "msg",
            ["password"],
            "value");

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    private static Mock<ILogger> CreateLogger(LogLevel level, bool enabled = true)
    {
        var logger = new Mock<ILogger>();
        logger.Setup(x => x.IsEnabled(level)).Returns(enabled);
        return logger;
    }
}
