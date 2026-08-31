using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Observability.Metrics;
using Mvp24Hours.Infrastructure.CronJob.Observability;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Observability;

[Trait("Category", "Unit")]
public class CronJobObservabilityExtensionsTest
{
    [Fact]
    public void AddCronJobMetrics_ShouldRegisterMetricsServiceAndInterface()
    {
        var services = new ServiceCollection();

        services.AddCronJobMetrics();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<CronJobMetricsService>().Should().NotBeNull();
        provider.GetRequiredService<ICronJobMetrics>().Should().BeOfType<CronJobMetricsService>();
        provider.GetRequiredService<CronJobMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobMetrics_Generic_ShouldRegisterCustomImplementation()
    {
        var services = new ServiceCollection();

        services.AddCronJobMetrics<CustomMetrics>();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICronJobMetrics>().Should().BeOfType<CustomMetrics>();
    }

    [Fact]
    public void AddCronJobHealthCheck_WithDefaults_ShouldRegisterCheckAndMetrics()
    {
        var services = new ServiceCollection();

        services.AddHealthChecks().AddCronJobHealthCheck();
        ServiceProvider provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.Should().ContainSingle(r =>
            r.Name == "cronjobs" &&
            r.FailureStatus == HealthStatus.Unhealthy &&
            r.Tags.Contains("cronjob") &&
            r.Tags.Contains("scheduled") &&
            r.Tags.Contains("background"));
        provider.GetRequiredService<ICronJobMetrics>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobHealthCheck_WithCustomNameStatusTagsAndTimeout_ShouldUseThem()
    {
        var services = new ServiceCollection();

        services.AddHealthChecks().AddCronJobHealthCheck(
            name: "custom-cronjobs",
            failureStatus: HealthStatus.Degraded,
            tags: ["custom"],
            timeout: TimeSpan.FromSeconds(5));
        ServiceProvider provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        options.Registrations.Should().ContainSingle(r =>
            r.Name == "custom-cronjobs" &&
            r.FailureStatus == HealthStatus.Degraded &&
            r.Tags.Contains("custom") &&
            r.Timeout == TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddCronJobHealthCheck_WithNullBuilder_ShouldThrow()
    {
        IHealthChecksBuilder builder = null!;

        Action act = () => builder.AddCronJobHealthCheck();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJobHealthCheck_WithConfigureOptions_ShouldBindOptionsAndRegisterCheck()
    {
        var services = new ServiceCollection();

        services.AddHealthChecks().AddCronJobHealthCheck(options => options.MaxFailureRate = 0.25);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<CronJobHealthCheckOptions>>().Value.MaxFailureRate.Should().Be(0.25);
        var checkOptions = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        checkOptions.Registrations.Should().ContainSingle(r => r.Name == "cronjobs");
    }

    [Fact]
    public void AddCronJobHealthCheck_WithConfigureOptionsAndNullBuilder_ShouldThrow()
    {
        IHealthChecksBuilder builder = null!;

        Action act = () => builder.AddCronJobHealthCheck(_ => { });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJobHealthCheck_WithNullConfigureOptions_ShouldThrow()
    {
        var services = new ServiceCollection();
        IHealthChecksBuilder builder = services.AddHealthChecks();

        Action act = () => builder.AddCronJobHealthCheck((Action<CronJobHealthCheckOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJobObservability_Parameterless_ShouldRegisterMetricsAndDefaultHealthCheckOptions()
    {
        var services = new ServiceCollection();

        services.AddCronJobObservability();
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICronJobMetrics>().Should().NotBeNull();
        provider.GetRequiredService<CronJobHealthCheckOptions>().Should().NotBeNull();
    }

    [Fact]
    public void AddCronJobObservability_WithNullServices_ShouldThrow()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddCronJobObservability();

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJobObservability_WithConfigureOptions_ShouldBindOptionsAndRegisterMetrics()
    {
        var services = new ServiceCollection();

        services.AddCronJobObservability(options => options.MaxFailureRate = 0.5);
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICronJobMetrics>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<CronJobHealthCheckOptions>>().Value.MaxFailureRate.Should().Be(0.5);
    }

    [Fact]
    public void AddCronJobObservability_WithConfigureOptionsAndNullServices_ShouldThrow()
    {
        IServiceCollection services = null!;

        Action act = () => services.AddCronJobObservability(_ => { });

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddCronJobObservability_WithNullConfigureOptions_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddCronJobObservability((Action<CronJobHealthCheckOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class CustomMetrics : ICronJobMetrics
    {
        public void RecordExecution(string jobName, double durationMs, bool success, int executionCount) { }
        public void RecordFailure(string jobName, Exception exception, double durationMs, int executionCount) { }
        public void RecordJobStarted(string jobName, string? cronExpression) { }
        public void RecordJobStopped(string jobName, long totalExecutions) { }
        public void RecordSkippedExecution(string jobName, string reason) { }
        public void RecordRetryAttempt(string jobName, int attemptNumber, int maxAttempts, double delayMs) { }
        public void RecordCircuitBreakerStateChange(string jobName, string previousState, string newState) { }
        public void IncrementActiveJob(string jobName) { }
        public void DecrementActiveJob(string jobName) { }
        public void RecordNextScheduledExecution(string jobName, DateTimeOffset nextExecution) { }
        public void RecordLastExecution(string jobName, DateTimeOffset lastExecution) { }
    }
}
