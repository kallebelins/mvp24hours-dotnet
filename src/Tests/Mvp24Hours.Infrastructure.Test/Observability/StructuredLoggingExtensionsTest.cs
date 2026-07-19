//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Observability.Logging;
using Mvp24Hours.Infrastructure.Testing.Logging;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class StructuredLoggingExtensionsTest
{
    private readonly FakeLogger<StructuredLoggingExtensionsTest> _logger = new();

    [Fact]
    public void LogOperation_OnSuccess_ShouldReturnValue()
    {
        int result = _logger.LogOperation("sync-op", () => 99);

        result.Should().Be(99);
        _logger.ContainsLog(LogLevel.Debug, "completed successfully").Should().BeTrue();
    }

    [Fact]
    public void LogOperation_OnFailure_ShouldRethrow()
    {
        var expected = new InvalidOperationException("sync failed");

        Action act = () => _logger.LogOperation<int>("sync-op-fail", () => throw expected);

        act.Should().Throw<InvalidOperationException>().WithMessage("sync failed");
        _logger.ContainsException<InvalidOperationException>().Should().BeTrue();
    }

    [Fact]
    public async Task LogOperationAsync_OnSuccess_ShouldReturnValue()
    {
        string result = await _logger.LogOperationAsync("async-op", () => Task.FromResult("ok"));

        result.Should().Be("ok");
        _logger.ContainsLog(LogLevel.Debug, "completed successfully").Should().BeTrue();
    }

    [Fact]
    public async Task LogOperationAsync_OnFailure_ShouldRethrow()
    {
        var expected = new InvalidOperationException("async failed");

        Func<Task> act = () => _logger.LogOperationAsync<int>(
            "async-op-fail",
            () => throw expected);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("async failed");
        _logger.ContainsException<InvalidOperationException>().Should().BeTrue();
    }

    [Fact]
    public void BeginOperation_OnDispose_ShouldNotThrow()
    {
        Action act = () =>
        {
            using IDisposable scope = _logger.BeginOperation("scoped-op");
        };

        act.Should().NotThrow();
        _logger.ContainsLog(LogLevel.Debug, "completed in").Should().BeTrue();
    }
}
