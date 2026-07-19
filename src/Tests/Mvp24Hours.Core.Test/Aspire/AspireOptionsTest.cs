//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Aspire;

namespace Mvp24Hours.Core.Test.Aspire;

/// <summary>
/// Unit tests for AspireOptions and related configuration classes.
/// </summary>
[Trait("Category", "Unit")]
public class AspireOptionsTest
{
    #region AspireOptions Default Values

    [Fact]
    public void AspireOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new AspireOptions();

        // Assert
        options.ServiceName.Should().BeNull();
        options.ServiceVersion.Should().BeNull();
        options.Environment.Should().BeNull();
        options.EnableOpenTelemetry.Should().BeTrue();
        options.EnableHealthChecks.Should().BeTrue();
        options.EnableResilience.Should().BeTrue();
        options.EnableServiceDiscovery.Should().BeTrue();
        options.OtlpEndpoint.Should().BeNull();
        options.ResourceAttributes.Should().NotBeNull();
        options.ResourceAttributes.Should().BeEmpty();
    }

    [Fact]
    public void AspireOptions_Telemetry_HasDefaultValues()
    {
        // Act
        var options = new AspireOptions();

        // Assert
        options.Telemetry.Should().NotBeNull();
        options.Telemetry.EnableLogging.Should().BeTrue();
        options.Telemetry.EnableTracing.Should().BeTrue();
        options.Telemetry.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void AspireOptions_HealthChecks_HasDefaultValues()
    {
        // Act
        var options = new AspireOptions();

        // Assert
        options.HealthChecks.Should().NotBeNull();
        options.HealthChecks.LivenessPath.Should().Be("/health/live");
        options.HealthChecks.ReadinessPath.Should().Be("/health/ready");
        options.HealthChecks.StartupPath.Should().Be("/health/startup");
        options.HealthChecks.TimeoutSeconds.Should().Be(5);
    }

    [Fact]
    public void AspireOptions_Resilience_HasDefaultValues()
    {
        // Act
        var options = new AspireOptions();

        // Assert
        options.Resilience.Should().NotBeNull();
        options.Resilience.EnableRetry.Should().BeTrue();
        options.Resilience.EnableCircuitBreaker.Should().BeTrue();
        options.Resilience.EnableTimeout.Should().BeTrue();
        options.Resilience.MaxRetryAttempts.Should().Be(3);
        options.Resilience.CircuitBreakerFailureThreshold.Should().Be(5);
        options.Resilience.CircuitBreakerBreakDurationSeconds.Should().Be(30);
        options.Resilience.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void AspireOptions_CanSetServiceName()
    {
        // Arrange
        var options = new AspireOptions();

        // Act
        options.ServiceName = "MyService";

        // Assert
        options.ServiceName.Should().Be("MyService");
    }

    [Fact]
    public void AspireOptions_CanSetServiceVersion()
    {
        // Arrange
        var options = new AspireOptions();

        // Act
        options.ServiceVersion = "2.0.0";

        // Assert
        options.ServiceVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void AspireOptions_CanSetOtlpEndpoint()
    {
        // Arrange
        var options = new AspireOptions();

        // Act
        options.OtlpEndpoint = "http://localhost:4317";

        // Assert
        options.OtlpEndpoint.Should().Be("http://localhost:4317");
    }

    [Fact]
    public void AspireOptions_CanDisableFeatures()
    {
        // Arrange
        var options = new AspireOptions
        {
            EnableOpenTelemetry = false,
            EnableHealthChecks = false,
            EnableResilience = false,
            EnableServiceDiscovery = false
        };

        // Assert
        options.EnableOpenTelemetry.Should().BeFalse();
        options.EnableHealthChecks.Should().BeFalse();
        options.EnableResilience.Should().BeFalse();
        options.EnableServiceDiscovery.Should().BeFalse();
    }

    [Fact]
    public void AspireOptions_ResourceAttributes_CanBeAdded()
    {
        // Arrange
        var options = new AspireOptions();

        // Act
        options.ResourceAttributes["deployment.environment"] = "production";
        options.ResourceAttributes["service.region"] = "us-east-1";

        // Assert
        options.ResourceAttributes.Should().HaveCount(2);
        options.ResourceAttributes["deployment.environment"].Should().Be("production");
    }

    #endregion

    #region AspireTelemetryOptions Tests

    [Fact]
    public void AspireTelemetryOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new AspireTelemetryOptions();

        // Assert
        options.EnableLogging.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.EnableAspNetCoreInstrumentation.Should().BeTrue();
        options.EnableHttpClientInstrumentation.Should().BeTrue();
        options.EnableEfCoreInstrumentation.Should().BeTrue();
        options.EnableMvp24HoursInstrumentation.Should().BeTrue();
        options.TraceSamplingRatio.Should().Be(1.0);
        options.AdditionalActivitySources.Should().NotBeNull();
        options.AdditionalMeterNames.Should().NotBeNull();
    }

    [Fact]
    public void AspireTelemetryOptions_CanSetSamplingRatio()
    {
        // Arrange
        var options = new AspireTelemetryOptions();

        // Act
        options.TraceSamplingRatio = 0.5;

        // Assert
        options.TraceSamplingRatio.Should().Be(0.5);
    }

    [Fact]
    public void AspireTelemetryOptions_AdditionalSources_CanBeAdded()
    {
        // Arrange
        var options = new AspireTelemetryOptions();

        // Act
        options.AdditionalActivitySources.Add("MyApp.Tracing");
        options.AdditionalMeterNames.Add("MyApp.Metrics");

        // Assert
        options.AdditionalActivitySources.Should().Contain("MyApp.Tracing");
        options.AdditionalMeterNames.Should().Contain("MyApp.Metrics");
    }

    #endregion

    #region AspireHealthCheckOptions Tests

    [Fact]
    public void AspireHealthCheckOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new AspireHealthCheckOptions();

        // Assert
        options.LivenessPath.Should().Be("/health/live");
        options.ReadinessPath.Should().Be("/health/ready");
        options.StartupPath.Should().Be("/health/startup");
        options.EnableDatabaseHealthChecks.Should().BeTrue();
        options.EnableCacheHealthChecks.Should().BeTrue();
        options.EnableMessagingHealthChecks.Should().BeTrue();
        options.TimeoutSeconds.Should().Be(5);
    }

    [Fact]
    public void AspireHealthCheckOptions_CanSetCustomPaths()
    {
        // Arrange
        var options = new AspireHealthCheckOptions
        {
            LivenessPath = "/live",
            ReadinessPath = "/ready",
            StartupPath = "/startup"
        };

        // Assert
        options.LivenessPath.Should().Be("/live");
        options.ReadinessPath.Should().Be("/ready");
        options.StartupPath.Should().Be("/startup");
    }

    #endregion

    #region AspireResilienceOptions Tests

    [Fact]
    public void AspireResilienceOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new AspireResilienceOptions();

        // Assert
        options.EnableRetry.Should().BeTrue();
        options.EnableCircuitBreaker.Should().BeTrue();
        options.EnableTimeout.Should().BeTrue();
        options.MaxRetryAttempts.Should().Be(3);
        options.CircuitBreakerFailureThreshold.Should().Be(5);
        options.CircuitBreakerBreakDurationSeconds.Should().Be(30);
        options.TimeoutSeconds.Should().Be(30);
    }

    [Fact]
    public void AspireResilienceOptions_CanSetCustomValues()
    {
        // Arrange
        var options = new AspireResilienceOptions
        {
            MaxRetryAttempts = 5,
            CircuitBreakerFailureThreshold = 10,
            TimeoutSeconds = 60
        };

        // Assert
        options.MaxRetryAttempts.Should().Be(5);
        options.CircuitBreakerFailureThreshold.Should().Be(10);
        options.TimeoutSeconds.Should().Be(60);
    }

    #endregion

    #region CorrelationIdAccessor Tests

    [Fact]
    public void CorrelationIdAccessor_InitialValue_IsNull()
    {
        // Arrange
        var accessor = new CorrelationIdAccessor();

        // Assert
        accessor.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void CorrelationIdAccessor_SetCorrelationId_StoresValue()
    {
        // Arrange
        var accessor = new CorrelationIdAccessor();
        string correlationId = "test-correlation-123";

        // Act
        accessor.SetCorrelationId(correlationId);

        // Assert
        accessor.CorrelationId.Should().Be(correlationId);
    }

    [Fact]
    public void CorrelationIdAccessor_SetCorrelationId_CanBeOverwritten()
    {
        // Arrange
        var accessor = new CorrelationIdAccessor();

        // Act
        accessor.SetCorrelationId("first-id");
        accessor.SetCorrelationId("second-id");

        // Assert
        accessor.CorrelationId.Should().Be("second-id");
    }

    [Fact]
    public void CorrelationIdAccessor_IsICorrelationIdAccessor()
    {
        // Act
        var accessor = new CorrelationIdAccessor();

        // Assert
        accessor.Should().BeAssignableTo<ICorrelationIdAccessor>();
    }

    #endregion
}
