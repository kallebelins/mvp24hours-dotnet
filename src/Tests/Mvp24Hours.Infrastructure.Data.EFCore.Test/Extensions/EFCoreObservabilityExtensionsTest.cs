using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;
using Mvp24Hours.Infrastructure.Data.EFCore.Resilience;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Extensions;

[Trait("Category", "Unit")]
public class EFCoreObservabilityExtensionsTest
{
    [Fact]
    public void AddMvp24HoursEFCoreMetrics_ShouldRegisterSingleton()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursEFCoreMetrics();

        using ServiceProvider provider = services.BuildServiceProvider();

        EFCoreMetrics metrics1 = provider.GetRequiredService<EFCoreMetrics>();
        EFCoreMetrics metrics2 = provider.GetRequiredService<EFCoreMetrics>();

        metrics1.Should().BeSameAs(metrics2);
    }

    [Fact]
    public void AddMvp24HoursEFCoreObservability_ShouldRegisterMetricsAndSlowQueryInterceptor()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursEFCoreObservability(o =>
        {
            o.SlowQueryThresholdMs = 250;
            o.EnableDiagnosticsListener = false;
            o.EnablePoolMonitoring = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        provider.GetRequiredService<EFCoreMetrics>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SlowQueryInterceptor>().Should().NotBeNull();
        provider.GetService<EFCoreDiagnosticsListener>().Should().BeNull();
    }

    [Fact]
    public void AddMvp24HoursEFCoreObservability_WithDiagnostics_ShouldRegisterListener()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursEFCoreObservability(o =>
        {
            o.EnableDiagnosticsListener = true;
            o.EnablePoolMonitoring = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<EFCoreDiagnosticsListener>().Should().NotBeNull();
    }

    [Fact]
    public void AddMvp24HoursEFCoreDevObservability_ShouldRegisterStructuredLogging()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursEFCoreDevObservability();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        provider.GetRequiredService<EFCoreMetrics>().Should().NotBeNull();
        provider.GetRequiredService<EFCoreDiagnosticsListener>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<SlowQueryInterceptor>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<StructuredLoggingInterceptor>().Should().NotBeNull();
        provider.GetServices<IHostedService>().Should().Contain(s => s is DbContextPoolMonitor);
    }

    [Fact]
    public void AddMvp24HoursSlowQueryInterceptor_ShouldResolve()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursEFCoreMetrics();
        services.AddMvp24HoursSlowQueryInterceptor(o => o.SlowQueryThresholdMs = 100);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<SlowQueryInterceptor>().Should().NotBeNull();
    }
}
