using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Observability;
using Mvp24Hours.Infrastructure.Testing.Assertions;
using Mvp24Hours.Infrastructure.Testing.Extensions;
using Mvp24Hours.Infrastructure.Testing.Fixtures;
using Mvp24Hours.Infrastructure.Testing.Logging;
using Mvp24Hours.Infrastructure.Testing.Observability;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Xunit;

namespace Mvp24Hours.Core.Test;

/// <summary>
/// Tests for the observability testing utilities (FakeLogger, FakeActivityListener, FakeMeterListener, assertions).
/// These tests demonstrate how to use the testing infrastructure for observability.
/// </summary>
public class ObservabilityTestingTest
{
    #region FakeLogger Tests

    [Fact]
    public void FakeLogger_ShouldCaptureLogEntries()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();

        // Act
        logger.LogInformation("Test message 1");
        logger.LogWarning("Test message 2");
        logger.LogError("Test error message");

        // Assert
        logger.LogCount.Should().Be(3);
        logger.ContainsLog(LogLevel.Information, "message 1").Should().BeTrue();
        logger.ContainsLog(LogLevel.Warning, "message 2").Should().BeTrue();
        logger.ContainsLog(LogLevel.Error, "error").Should().BeTrue();
    }

    [Fact]
    public void FakeLogger_ShouldCaptureStructuredLogging()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();

        // Act
        logger.LogInformation("User {UserId} performed {Action}", "user-123", "Login");

        // Assert
        logger.ContainsLog("User user-123 performed Login").Should().BeTrue();
        logger.Logs[0].State.Should().NotBeNull();
    }

    [Fact]
    public void FakeLogger_ShouldCaptureExceptions()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        var exception = new InvalidOperationException("Test exception");

        // Act
        logger.LogError(exception, "An error occurred");

        // Assert
        logger.ContainsException<InvalidOperationException>().Should().BeTrue();
        logger.GetLogsWithExceptions().Should().HaveCount(1);
    }

    [Fact]
    public void FakeLogger_ShouldCaptureScopes()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();

        // Act
        using (logger.BeginScope("Outer scope"))
        {
            using (logger.BeginScope("Inner scope"))
            {
                logger.LogInformation("Message inside scopes");
            }
        }

        // Assert
        var log = logger.Logs[0];
        log.Scopes.Should().HaveCount(2);
    }

    [Fact]
    public void FakeLogger_GetLogs_ShouldFilterByLevel()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogDebug("Debug 1");
        logger.LogInformation("Info 1");
        logger.LogInformation("Info 2");
        logger.LogWarning("Warning 1");
        logger.LogError("Error 1");

        // Act
        var debugLogs = logger.GetLogs(LogLevel.Debug);
        var infoLogs = logger.GetLogs(LogLevel.Information);
        var warningsAndAbove = logger.GetLogsAtOrAbove(LogLevel.Warning);

        // Assert
        debugLogs.Should().HaveCount(1);
        infoLogs.Should().HaveCount(2);
        warningsAndAbove.Should().HaveCount(2);
    }

    [Fact]
    public void FakeLogger_Clear_ShouldRemoveAllLogs()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogInformation("Test 1");
        logger.LogInformation("Test 2");

        // Act
        logger.Clear();

        // Assert
        logger.LogCount.Should().Be(0);
        logger.Logs.Should().BeEmpty();
    }

    [Fact]
    public void FakeLogger_MinimumLevel_ShouldFilterLogs()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>
        {
            MinimumLevel = LogLevel.Warning
        };

        // Act
        logger.LogDebug("Debug");
        logger.LogInformation("Info");
        logger.LogWarning("Warning");
        logger.LogError("Error");

        // Assert
        logger.LogCount.Should().Be(2);
        logger.ContainsLog(LogLevel.Debug, "Debug").Should().BeFalse();
        logger.ContainsLog(LogLevel.Warning, "Warning").Should().BeTrue();
    }

    #endregion

    #region InMemoryLoggerProvider Tests

    [Fact]
    public void InMemoryLoggerProvider_ShouldCaptureLogsFromMultipleCategories()
    {
        // Arrange
        var provider = new InMemoryLoggerProvider();
        var logger1 = provider.CreateLogger("Category1");
        var logger2 = provider.CreateLogger("Category2");

        // Act
        logger1.LogInformation("Message from Category1");
        logger2.LogWarning("Message from Category2");

        // Assert
        provider.LogCount.Should().Be(2);
        provider.ContainsLogInCategory("Category1", "Message from Category1").Should().BeTrue();
        provider.ContainsLogInCategory("Category2", "Message from Category2").Should().BeTrue();
    }

    [Fact]
    public void InMemoryLoggerProvider_GetLogsForCategory_ShouldFilterCorrectly()
    {
        // Arrange
        var provider = new InMemoryLoggerProvider();
        var orderLogger = provider.CreateLogger("OrderService");
        var paymentLogger = provider.CreateLogger("PaymentService");

        orderLogger.LogInformation("Order created");
        orderLogger.LogInformation("Order updated");
        paymentLogger.LogInformation("Payment processed");

        // Act
        var orderLogs = provider.GetLogsForCategory("OrderService");
        var paymentLogs = provider.GetLogsForCategory("PaymentService");

        // Assert
        orderLogs.Should().HaveCount(2);
        paymentLogs.Should().HaveCount(1);
    }

    [Fact]
    public void InMemoryLoggerProvider_HasErrors_ShouldDetectErrors()
    {
        // Arrange
        var provider = new InMemoryLoggerProvider();
        var logger = provider.CreateLogger("TestCategory");

        // Act
        logger.LogInformation("Info");
        logger.LogWarning("Warning");

        // Assert - No errors yet
        provider.HasErrors().Should().BeFalse();

        // Act - Add error
        logger.LogError("Error");

        // Assert - Now has errors
        provider.HasErrors().Should().BeTrue();
    }

    [Fact]
    public void InMemoryLoggerProvider_WithDependencyInjection_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddInMemoryLoggerProvider();
        var provider = services.BuildServiceProvider();

        var loggerProvider = provider.GetRequiredService<InMemoryLoggerProvider>();
        var logger = provider.GetRequiredService<ILogger<ObservabilityTestingTest>>();

        // Act
        logger.LogInformation("Test from DI");

        // Assert
        loggerProvider.ContainsLog("Test from DI").Should().BeTrue();
    }

    #endregion

    #region LogAssertions Tests

    [Fact]
    public void LogAssertions_AssertLogged_ShouldPassWhenLogExists()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogInformation("Expected message");

        // Act & Assert - Should not throw
        LogAssertions.AssertLogged(logger, LogLevel.Information, "Expected message");
    }

    [Fact]
    public void LogAssertions_AssertLogged_ShouldThrowWhenLogMissing()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogInformation("Some message");

        // Act & Assert
        var action = () => LogAssertions.AssertLogged(logger, LogLevel.Error, "Missing message");
        action.Should().Throw<AssertionException>()
            .WithMessage("*Error log*Missing message*");
    }

    [Fact]
    public void LogAssertions_AssertNoErrorsLogged_ShouldPassWhenNoErrors()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogInformation("Info");
        logger.LogWarning("Warning");

        // Act & Assert - Should not throw
        LogAssertions.AssertNoErrorsLogged(logger);
    }

    [Fact]
    public void LogAssertions_AssertNoErrorsLogged_ShouldThrowWhenErrorExists()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogError("An error occurred");

        // Act & Assert
        var action = () => LogAssertions.AssertNoErrorsLogged(logger);
        action.Should().Throw<AssertionException>()
            .WithMessage("*Expected no errors*");
    }

    [Fact]
    public void LogAssertions_AssertLoggedCount_ShouldVerifyCount()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogDebug("Debug 1");
        logger.LogDebug("Debug 2");
        logger.LogDebug("Debug 3");

        // Act & Assert - Should not throw
        LogAssertions.AssertLoggedCount(logger, LogLevel.Debug, 3);
    }

    [Fact]
    public void LogAssertions_AssertLoggedException_ShouldVerifyExceptionType()
    {
        // Arrange
        var logger = new FakeLogger<ObservabilityTestingTest>();
        logger.LogError(new InvalidOperationException("Test"), "Error");

        // Act & Assert - Should not throw
        LogAssertions.AssertLoggedException<InvalidOperationException>(logger);
    }

    #endregion

    #region FakeActivityListener Tests

    [Fact]
    public void FakeActivityListener_ShouldCaptureActivities()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");

        // Act
        using (var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("TestOperation"))
        {
            activity?.SetTag("test.tag", "test-value");
        }

        // Assert
        listener.ActivityCount.Should().Be(1);
        listener.HasActivity("TestOperation").Should().BeTrue();
    }

    [Fact]
    public void FakeActivityListener_ShouldCaptureActivityDetails()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");

        // Act
        using (var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("DetailedOperation", ActivityKind.Internal))
        {
            activity?.SetTag("custom.tag", "custom-value");
            activity?.AddEvent(new ActivityEvent("test-event"));
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        // Assert
        var recorded = listener.GetActivities("DetailedOperation").First();
        recorded.Kind.Should().Be(ActivityKind.Internal);
        recorded.HasTag("custom.tag").Should().BeTrue();
        recorded.GetTag("custom.tag").Should().Be("custom-value");
        recorded.HasEvent("test-event").Should().BeTrue();
        recorded.Status.Should().Be(ActivityStatusCode.Ok);
    }

    [Fact]
    public void FakeActivityListener_ShouldFilterBySourceName()
    {
        // Arrange - Listen only to Pipe module
        using var listener = new FakeActivityListener("Mvp24Hours.Pipe");

        // Act
        using (Mvp24HoursActivitySources.Core.Source.StartActivity("CoreActivity")) { }
        using (Mvp24HoursActivitySources.Pipe.Source.StartActivity("PipeActivity")) { }

        // Assert
        listener.ActivityCount.Should().Be(1);
        listener.HasActivity("PipeActivity").Should().BeTrue();
        listener.HasActivity("CoreActivity").Should().BeFalse();
    }

    [Fact]
    public void FakeActivityListener_GetErrorActivities_ShouldReturnOnlyErrors()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");

        // Act
        using (var successActivity = Mvp24HoursActivitySources.Core.Source.StartActivity("Success"))
        {
            successActivity?.SetStatus(ActivityStatusCode.Ok);
        }

        using (var errorActivity = Mvp24HoursActivitySources.Core.Source.StartActivity("Error"))
        {
            errorActivity?.SetStatus(ActivityStatusCode.Error, "Something failed");
        }

        // Assert
        listener.HasErrors().Should().BeTrue();
        var errors = listener.GetErrorActivities();
        errors.Should().HaveCount(1);
        errors[0].OperationName.Should().Be("Error");
    }

    #endregion

    #region ActivityAssertions Tests

    [Fact]
    public void ActivityAssertions_AssertActivityRecorded_ShouldPassWhenExists()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");
        using (Mvp24HoursActivitySources.Core.Source.StartActivity("ExpectedActivity")) { }

        // Act & Assert - Should not throw
        ActivityAssertions.AssertActivityRecorded(listener, "ExpectedActivity");
    }

    [Fact]
    public void ActivityAssertions_AssertActivityRecorded_ShouldThrowWhenMissing()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");
        using (Mvp24HoursActivitySources.Core.Source.StartActivity("SomeActivity")) { }

        // Act & Assert
        var action = () => ActivityAssertions.AssertActivityRecorded(listener, "MissingActivity");
        action.Should().Throw<AssertionException>()
            .WithMessage("*MissingActivity*");
    }

    [Fact]
    public void ActivityAssertions_AssertNoErrorActivities_ShouldPassWhenNoErrors()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");
        using (var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("Success"))
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
        }

        // Act & Assert - Should not throw
        ActivityAssertions.AssertNoErrorActivities(listener);
    }

    [Fact]
    public void ActivityAssertions_AssertActivityHasTag_ShouldVerifyTags()
    {
        // Arrange
        using var listener = new FakeActivityListener("Mvp24Hours.*");
        using (var activity = Mvp24HoursActivitySources.Core.Source.StartActivity("TaggedActivity"))
        {
            activity?.SetTag("user.id", "user-123");
        }

        // Act & Assert - Should not throw
        ActivityAssertions.AssertActivityHasTag(listener, "TaggedActivity", "user.id", "user-123");
    }

    #endregion

    #region FakeMeterListener Tests

    [Fact]
    public void FakeMeterListener_ShouldCaptureMeasurements()
    {
        // Arrange
        var meter = new Meter("TestMeter", "1.0.0");
        var counter = meter.CreateCounter<int>("test_counter", "items", "Test counter");
        
        using var listener = new FakeMeterListener("TestMeter");

        // Act
        counter.Add(5, new KeyValuePair<string, object?>("region", "us-west"));
        counter.Add(3, new KeyValuePair<string, object?>("region", "us-east"));

        // Assert
        listener.MeasurementCount.Should().Be(2);
        listener.HasMeasurement("test_counter").Should().BeTrue();
        listener.GetSum("test_counter").Should().Be(8);
    }

    [Fact]
    public void FakeMeterListener_ShouldCaptureHistogramMeasurements()
    {
        // Arrange
        var meter = new Meter("HistogramMeter", "1.0.0");
        var histogram = meter.CreateHistogram<double>("request_duration", "ms", "Request duration");
        
        using var listener = new FakeMeterListener("HistogramMeter");

        // Act
        histogram.Record(10.5);
        histogram.Record(20.3);
        histogram.Record(15.7);

        // Assert
        listener.GetCount("request_duration").Should().Be(3);
        listener.GetAverage("request_duration").Should().BeApproximately(15.5, 0.1);
    }

    [Fact]
    public void FakeMeterListener_ShouldCaptureTags()
    {
        // Arrange
        var meter = new Meter("TagMeter", "1.0.0");
        var counter = meter.CreateCounter<int>("tagged_counter");
        
        using var listener = new FakeMeterListener("TagMeter");

        // Act
        counter.Add(1, 
            new KeyValuePair<string, object?>("operation", "read"),
            new KeyValuePair<string, object?>("success", true));

        // Assert
        var measurement = listener.GetMeasurements("tagged_counter").First();
        measurement.HasTag("operation").Should().BeTrue();
        measurement.GetTag("operation").Should().Be("read");
        measurement.GetTag("success").Should().Be(true);
    }

    [Fact]
    public void FakeMeterListener_WithFilter_ShouldOnlyCaptureMatcher()
    {
        // Arrange
        var meter1 = new Meter("MyApp.Orders", "1.0.0");
        var meter2 = new Meter("MyApp.Payments", "1.0.0");
        var counter1 = meter1.CreateCounter<int>("orders_total");
        var counter2 = meter2.CreateCounter<int>("payments_total");
        
        using var listener = new FakeMeterListener("MyApp.Orders");

        // Act
        counter1.Add(1);
        counter2.Add(1);

        // Assert
        listener.MeasurementCount.Should().Be(1);
        listener.HasMeasurement("orders_total").Should().BeTrue();
        listener.HasMeasurement("payments_total").Should().BeFalse();
    }

    #endregion

    #region MetricAssertions Tests

    [Fact]
    public void MetricAssertions_AssertMetricRecorded_ShouldPassWhenExists()
    {
        // Arrange
        var meter = new Meter("AssertMeter1", "1.0.0");
        var counter = meter.CreateCounter<int>("expected_metric");
        using var listener = new FakeMeterListener("AssertMeter1");
        
        counter.Add(1);

        // Act & Assert - Should not throw
        MetricAssertions.AssertMetricRecorded(listener, "expected_metric");
    }

    [Fact]
    public void MetricAssertions_AssertCounterValue_ShouldVerifySum()
    {
        // Arrange
        var meter = new Meter("AssertMeter2", "1.0.0");
        var counter = meter.CreateCounter<int>("operations_total");
        using var listener = new FakeMeterListener("AssertMeter2");
        
        counter.Add(5);
        counter.Add(3);
        counter.Add(2);

        // Act & Assert - Should not throw
        MetricAssertions.AssertCounterValue(listener, "operations_total", 10);
    }

    [Fact]
    public void MetricAssertions_AssertMetricHasTag_ShouldVerifyTags()
    {
        // Arrange
        var meter = new Meter("AssertMeter3", "1.0.0");
        var counter = meter.CreateCounter<int>("tagged_metric");
        using var listener = new FakeMeterListener("AssertMeter3");
        
        counter.Add(1, new KeyValuePair<string, object?>("environment", "production"));

        // Act & Assert - Should not throw
        MetricAssertions.AssertMetricHasTag(listener, "tagged_metric", "environment", "production");
    }

    #endregion

    #region ObservabilityTestFixture Tests

    [Fact]
    public void ObservabilityTestFixture_ShouldProvideAllComponents()
    {
        // Arrange & Act
        using var fixture = new ObservabilityTestFixture("Mvp24Hours.*");

        // Assert
        fixture.LoggerProvider.Should().NotBeNull();
        fixture.ActivityListener.Should().NotBeNull();
        fixture.MeterListener.Should().NotBeNull();
    }

    [Fact]
    public void ObservabilityTestFixture_Reset_ShouldClearAllData()
    {
        // Arrange
        using var fixture = new ObservabilityTestFixture("Mvp24Hours.*");
        
        var logger = (ILogger)fixture.LoggerProvider.CreateLogger("Test");
        logger.LogInformation("Test log");
        
        using (Mvp24HoursActivitySources.Core.Source.StartActivity("TestActivity")) { }

        fixture.LoggerProvider.LogCount.Should().BeGreaterThan(0);
        fixture.ActivityListener.ActivityCount.Should().BeGreaterThan(0);

        // Act
        fixture.Reset();

        // Assert
        fixture.LoggerProvider.LogCount.Should().Be(0);
        fixture.ActivityListener.ActivityCount.Should().Be(0);
        fixture.MeterListener.MeasurementCount.Should().Be(0);
    }

    [Fact]
    public void ObservabilityTestFixture_GetSummary_ShouldReturnOverview()
    {
        // Arrange
        using var fixture = new ObservabilityTestFixture("Mvp24Hours.*");
        
        var logger = (ILogger)fixture.LoggerProvider.CreateLogger("Test");
        logger.LogInformation("Test log");
        logger.LogError("Error log");

        // Act
        var summary = fixture.GetSummary();

        // Assert
        summary.Should().Contain("Observability Summary:");
        summary.Should().Contain("Logs:");
        summary.Should().Contain("Activities:");
        summary.Should().Contain("Metrics:");
    }

    #endregion

    #region DI Integration Tests

    [Fact]
    public void AddObservabilityTesting_ShouldRegisterAllComponents()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddObservabilityTesting("Mvp24Hours.*");
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<InMemoryLoggerProvider>().Should().NotBeNull();
        provider.GetService<FakeActivityListener>().Should().NotBeNull();
        provider.GetService<FakeMeterListener>().Should().NotBeNull();
    }

    [Fact]
    public void AddFakeActivityListener_ShouldRegisterWithFilter()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Act
        services.AddFakeActivityListener("Mvp24Hours.Pipe");
        var provider = services.BuildServiceProvider();
        var listener = provider.GetRequiredService<FakeActivityListener>();

        // Act - Create activities
        using (Mvp24HoursActivitySources.Pipe.Source.StartActivity("PipeActivity")) { }

        // Assert
        listener.HasActivity("PipeActivity").Should().BeTrue();
    }

    #endregion
}

