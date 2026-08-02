//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Observability;
using Mvp24Hours.Infrastructure.Observability.Contract;
using Mvp24Hours.Infrastructure.Observability.Extensions;

namespace Mvp24Hours.Infrastructure.Test.Observability;

[Trait("Category", "Unit")]
public class ObservabilityServiceExtensionsTest
{
    [Fact]
    public void AddInfrastructureObservability_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => ObservabilityServiceExtensions.AddInfrastructureObservability(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddInfrastructureObservability_ShouldRegisterIInfrastructureDiagnosticsAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureObservability();

        ServiceProvider sp = services.BuildServiceProvider();

        IInfrastructureDiagnostics first = sp.GetRequiredService<IInfrastructureDiagnostics>();
        IInfrastructureDiagnostics second = sp.GetRequiredService<IInfrastructureDiagnostics>();

        first.Should().BeOfType<InfrastructureDiagnostics>();
        first.Should().BeSameAs(second);
    }

    [Fact]
    public void AddInfrastructureObservability_WithConfigureCallback_ShouldInvokeCallback()
    {
        var services = new ServiceCollection();
        ObservabilityOptions? capturedOptions = null;

        services.AddInfrastructureObservability(options =>
        {
            capturedOptions = options;
            options.EnableDetailedLogging = false;
            options.EnableMetrics = false;
        });

        capturedOptions.Should().NotBeNull();
        capturedOptions!.EnableDetailedLogging.Should().BeFalse();
        capturedOptions.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void AddSubsystemDiagnostics_ShouldRegisterProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureObservability();
        services.AddSubsystemDiagnostics<TestSubsystemProvider>();

        ServiceProvider sp = services.BuildServiceProvider();

        ISubsystemDiagnosticsProvider provider = sp.GetRequiredService<ISubsystemDiagnosticsProvider>();

        provider.Should().BeOfType<TestSubsystemProvider>();
        provider.SubsystemName.Should().Be("Test");
    }

    [Fact]
    public void AddSubsystemDiagnostics_WithNullServices_ShouldThrowArgumentNullException()
    {
        Action act = () => ObservabilityServiceExtensions.AddSubsystemDiagnostics<TestSubsystemProvider>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void ObservabilityOptions_Defaults_ShouldEnableAllFeatures()
    {
        var options = new ObservabilityOptions();

        options.EnableDetailedLogging.Should().BeTrue();
        options.EnableCorrelationIdPropagation.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
    }

    private sealed class TestSubsystemProvider : ISubsystemDiagnosticsProvider
    {
        public string SubsystemName => "Test";

        public Task<SubsystemDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SubsystemDiagnostics
            {
                SubsystemName = "Test",
                Health = SubsystemHealth.Healthy
            });
        }

        public Task<Dictionary<string, object>> GetMetricsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Dictionary<string, object> { ["count"] = 1 });
        }

        public Task<IReadOnlyList<ErrorInfo>> GetRecentErrorsAsync(
            int maxErrors = 10,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ErrorInfo>>([]);
        }
    }
}
