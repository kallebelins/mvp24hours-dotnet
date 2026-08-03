//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Logging;
using AssertionException = Mvp24Hours.Infrastructure.Testing.Assertions.AssertionException;

namespace Mvp24Hours.Infrastructure.Test.Testing.Assertions;

[Trait("Category", "Unit")]
public class LogAssertionsTest
{
    #region FakeLogger

    [Fact]
    public void AssertLogged_WithLevelAndMessage_ShouldPassWhenMatchExists()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("Processing order 42 started");

        Action act = () => LogAssertions.AssertLogged(logger, LogLevel.Information, "order 42");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLogged_WithLevelAndMessage_ShouldThrowWhenMissing()
    {
        FakeLogger logger = new("Test");
        logger.LogDebug("Unrelated message");

        Action act = () => LogAssertions.AssertLogged(logger, LogLevel.Information, "order 42");

        act.Should().Throw<AssertionException>().WithMessage("*Information*order 42*");
    }

    [Fact]
    public void AssertLogged_WithMessageOnly_ShouldPassAtAnyLevel()
    {
        FakeLogger logger = new("Test");
        logger.LogWarning("Something noteworthy happened");

        Action act = () => LogAssertions.AssertLogged(logger, "noteworthy");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLogged_WithMessageOnly_ShouldThrowWhenMissing()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("Other");

        Action act = () => LogAssertions.AssertLogged(logger, "noteworthy");

        act.Should().Throw<AssertionException>().WithMessage("*noteworthy*");
    }

    [Fact]
    public void AssertLoggedCount_ShouldPassWhenCountMatches()
    {
        FakeLogger logger = new("Test");
        logger.LogDebug("a");
        logger.LogDebug("b");

        Action act = () => LogAssertions.AssertLoggedCount(logger, LogLevel.Debug, 2);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLoggedCount_ShouldThrowWhenCountMismatch()
    {
        FakeLogger logger = new("Test");
        logger.LogDebug("a");

        Action act = () => LogAssertions.AssertLoggedCount(logger, LogLevel.Debug, 3);

        act.Should().Throw<AssertionException>().WithMessage("*Expected 3 Debug*");
    }

    [Fact]
    public void AssertLoggedAtLevel_ShouldPassWhenLevelExists()
    {
        FakeLogger logger = new("Test");
        logger.LogError("Failure");

        Action act = () => LogAssertions.AssertLoggedAtLevel(logger, LogLevel.Error);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLoggedAtLevel_ShouldThrowWhenLevelMissing()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("ok");

        Action act = () => LogAssertions.AssertLoggedAtLevel(logger, LogLevel.Error);

        act.Should().Throw<AssertionException>().WithMessage("*Expected at least one Error*");
    }

    [Fact]
    public void AssertNoErrorsLogged_ShouldPassWhenNoErrors()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("ok");
        logger.LogWarning("heads up");

        Action act = () => LogAssertions.AssertNoErrorsLogged(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoErrorsLogged_ShouldThrowWhenErrorExists()
    {
        FakeLogger logger = new("Test");
        logger.LogError("boom");

        Action act = () => LogAssertions.AssertNoErrorsLogged(logger);

        act.Should().Throw<AssertionException>().WithMessage("*Expected no errors*");
    }

    [Fact]
    public void AssertNoWarningsOrErrorsLogged_ShouldPassWhenClean()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("ok");

        Action act = () => LogAssertions.AssertNoWarningsOrErrorsLogged(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoWarningsOrErrorsLogged_ShouldThrowWhenWarningExists()
    {
        FakeLogger logger = new("Test");
        logger.LogWarning("careful");

        Action act = () => LogAssertions.AssertNoWarningsOrErrorsLogged(logger);

        act.Should().Throw<AssertionException>().WithMessage("*warnings or errors*");
    }

    [Fact]
    public void AssertLoggedException_ShouldPassWhenExceptionLogged()
    {
        FakeLogger logger = new("Test");
        logger.LogError(new InvalidOperationException("bad"), "failed");

        Action act = () => LogAssertions.AssertLoggedException<InvalidOperationException>(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLoggedException_ShouldThrowWhenExceptionMissing()
    {
        FakeLogger logger = new("Test");
        logger.LogError("no exception");

        Action act = () => LogAssertions.AssertLoggedException<InvalidOperationException>(logger);

        act.Should().Throw<AssertionException>().WithMessage("*InvalidOperationException*");
    }

    [Fact]
    public void AssertNoLogsRecorded_ShouldPassWhenEmpty()
    {
        FakeLogger logger = new("Test");

        Action act = () => LogAssertions.AssertNoLogsRecorded(logger);

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertNoLogsRecorded_ShouldThrowWhenLogsExist()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("hello");

        Action act = () => LogAssertions.AssertNoLogsRecorded(logger);

        act.Should().Throw<AssertionException>().WithMessage("*Expected no logs*");
    }

    [Fact]
    public void AssertLogged_WithPredicate_ShouldPassWhenMatchExists()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("User 99 logged in");

        Action act = () => LogAssertions.AssertLogged(logger, e => e.Message.Contains("99"), "with user id");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLogged_WithPredicate_ShouldThrowWhenNoMatch()
    {
        FakeLogger logger = new("Test");
        logger.LogInformation("hello");

        Action act = () => LogAssertions.AssertLogged(logger, e => e.Message.Contains("99"));

        act.Should().Throw<AssertionException>().WithMessage("*matching predicate*");
    }

    #endregion

    #region InMemoryLoggerProvider

    [Fact]
    public void Provider_AssertLogged_ShouldPassWhenMatchExists()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("OrderService");
        logger.LogInformation("Order created");

        Action act = () => LogAssertions.AssertLogged(provider, LogLevel.Information, "Order created");

        act.Should().NotThrow();
    }

    [Fact]
    public void Provider_AssertLogged_ShouldThrowWhenMissing()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("OrderService");
        logger.LogDebug("other");

        Action act = () => LogAssertions.AssertLogged(provider, LogLevel.Information, "Order created");

        act.Should().Throw<AssertionException>().WithMessage("*Order created*");
    }

    [Fact]
    public void AssertLoggedInCategory_ShouldPassWhenCategoryAndMessageMatch()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("MyApp.OrderService");
        logger.LogInformation("Processing started");

        Action act = () => LogAssertions.AssertLoggedInCategory(provider, "OrderService", "Processing");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertLoggedInCategory_ShouldThrowWhenMissing()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("MyApp.OtherService");
        logger.LogInformation("Unrelated");

        Action act = () => LogAssertions.AssertLoggedInCategory(provider, "OrderService", "Processing");

        act.Should().Throw<AssertionException>().WithMessage("*OrderService*Processing*");
    }

    [Fact]
    public void Provider_AssertNoErrorsLogged_ShouldPassWhenClean()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("Svc");
        logger.LogInformation("ok");

        Action act = () => LogAssertions.AssertNoErrorsLogged(provider);

        act.Should().NotThrow();
    }

    [Fact]
    public void Provider_AssertNoErrorsLogged_ShouldThrowWhenErrorExists()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("Svc");
        logger.LogCritical("fatal");

        Action act = () => LogAssertions.AssertNoErrorsLogged(provider);

        act.Should().Throw<AssertionException>().WithMessage("*Expected no errors*");
    }

    [Fact]
    public void Provider_AssertNoWarningsOrErrorsLogged_ShouldThrowWhenWarningExists()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("Svc");
        logger.LogWarning("warn");

        Action act = () => LogAssertions.AssertNoWarningsOrErrorsLogged(provider);

        act.Should().Throw<AssertionException>().WithMessage("*warnings or errors*");
    }

    [Fact]
    public void AssertCategoryHasLogs_ShouldPassWhenCategoryHasEntries()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("Billing.InvoiceService");
        logger.LogInformation("sent");

        Action act = () => LogAssertions.AssertCategoryHasLogs(provider, "InvoiceService");

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertCategoryHasLogs_ShouldThrowWhenCategoryEmpty()
    {
        using InMemoryLoggerProvider provider = new();
        ILogger logger = provider.CreateLogger("OtherService");
        logger.LogInformation("sent");

        Action act = () => LogAssertions.AssertCategoryHasLogs(provider, "InvoiceService");

        act.Should().Throw<AssertionException>().WithMessage("*InvoiceService*");
    }

    #endregion

    [Fact]
    public void NullArguments_ShouldThrowArgumentNullException()
    {
        FakeLogger logger = new();
        using InMemoryLoggerProvider provider = new();

        Action nullLogger = () => LogAssertions.AssertLogged((FakeLogger)null!, LogLevel.Information, "x");
        Action nullMessage = () => LogAssertions.AssertLogged(logger, LogLevel.Information, null!);
        Action nullPredicate = () => LogAssertions.AssertLogged(logger, (Func<LogEntry, bool>)null!);
        Action nullProvider = () => LogAssertions.AssertLogged((InMemoryLoggerProvider)null!, LogLevel.Information, "x");
        Action nullCategory = () => LogAssertions.AssertLoggedInCategory(provider, null!, "x");

        nullLogger.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        nullMessage.Should().Throw<ArgumentNullException>().WithParameterName("messageContains");
        nullPredicate.Should().Throw<ArgumentNullException>().WithParameterName("predicate");
        nullProvider.Should().Throw<ArgumentNullException>().WithParameterName("provider");
        nullCategory.Should().Throw<ArgumentNullException>().WithParameterName("categoryContains");
    }
}
