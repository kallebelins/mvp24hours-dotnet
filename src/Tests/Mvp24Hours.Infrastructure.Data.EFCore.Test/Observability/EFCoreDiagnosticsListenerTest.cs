using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Observability;

[Trait("Category", "Unit")]
public class EFCoreDiagnosticsListenerTest
{
    [Fact]
    public void DiagnosticListenerName_ShouldBeEfCore()
    {
        EFCoreDiagnosticsListener.DiagnosticListenerName.Should().Be("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Subscribe_ShouldNotThrow()
    {
        using var metrics = new EFCoreMetrics();
        using var listener = new EFCoreDiagnosticsListener(
            NullLogger<EFCoreDiagnosticsListener>.Instance,
            metrics);

        Action act = () => listener.Subscribe();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        using var listener = new EFCoreDiagnosticsListener();
        listener.Subscribe();

        Action act = () =>
        {
            listener.Dispose();
            listener.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnCompleted_AndOnError_ShouldNotThrow()
    {
        using var listener = new EFCoreDiagnosticsListener(
            NullLogger<EFCoreDiagnosticsListener>.Instance);

        Action completed = () => listener.OnCompleted();
        Action error = () => listener.OnError(new InvalidOperationException("diag"));

        completed.Should().NotThrow();
        error.Should().NotThrow();
    }

    [Fact]
    public void OnNext_WithUnknownEvent_ShouldNotThrow()
    {
        using var listener = new EFCoreDiagnosticsListener();

        Action act = () => listener.OnNext(new KeyValuePair<string, object?>("Unknown.Event", null));
        act.Should().NotThrow();
    }
}
