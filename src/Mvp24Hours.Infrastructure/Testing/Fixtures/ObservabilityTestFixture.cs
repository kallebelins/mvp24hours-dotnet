//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Testing.Logging;
using Mvp24Hours.Infrastructure.Testing.Observability;
using System;

namespace Mvp24Hours.Infrastructure.Testing.Fixtures
{
    /// <summary>
    /// Test fixture for observability integration tests.
    /// Provides pre-configured logging, tracing, and metrics capture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixture provides:
    /// - InMemoryLoggerProvider for capturing logs across all categories
    /// - FakeActivityListener for capturing distributed tracing spans
    /// - FakeMeterListener for capturing metrics
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyObservabilityTests : IClassFixture&lt;ObservabilityTestFixture&gt;
    /// {
    ///     private readonly ObservabilityTestFixture _fixture;
    ///     
    ///     public MyObservabilityTests(ObservabilityTestFixture fixture)
    ///     {
    ///         _fixture = fixture;
    ///         _fixture.Reset();
    ///     }
    ///     
    ///     [Fact]
    ///     public async Task Should_Log_And_Trace_Operations()
    ///     {
    ///         var service = _fixture.GetService&lt;MyService&gt;();
    ///         await service.ProcessAsync();
    ///         
    ///         // Assert logging
    ///         Assert.True(_fixture.LoggerProvider.ContainsLog(LogLevel.Information, "Processing"));
    ///         
    ///         // Assert tracing
    ///         Assert.True(_fixture.ActivityListener.HasActivity("Mvp24Hours.Pipe.Pipeline"));
    ///         
    ///         // Assert metrics
    ///         Assert.True(_fixture.MeterListener.HasMeasurement("mvp24hours.pipe.operations_total"));
    ///     }
    /// }
    /// </code>
    /// </example>
    public class ObservabilityTestFixture : IDisposable
    {
        private ServiceProvider? _serviceProvider;
        private readonly ServiceCollection _services;
        private bool _disposed;

        /// <summary>
        /// Gets the in-memory logger provider for capturing logs.
        /// </summary>
        public InMemoryLoggerProvider LoggerProvider { get; }

        /// <summary>
        /// Gets the fake activity listener for capturing spans.
        /// </summary>
        public FakeActivityListener ActivityListener { get; }

        /// <summary>
        /// Gets the fake meter listener for capturing metrics.
        /// </summary>
        public FakeMeterListener MeterListener { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ObservabilityTestFixture"/> class.
        /// </summary>
        public ObservabilityTestFixture() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance with an optional source filter for activities and meters.
        /// </summary>
        /// <param name="sourceFilter">
        /// Optional filter for source names. Supports wildcard (*) at the end.
        /// Examples: "Mvp24Hours.*", null (all sources).
        /// </param>
        public ObservabilityTestFixture(string? sourceFilter)
        {
            LoggerProvider = new InMemoryLoggerProvider();
            ActivityListener = new FakeActivityListener(sourceFilter);
            MeterListener = new FakeMeterListener(sourceFilter);

            _services = new ServiceCollection();
            ConfigureServices(_services);
        }

        /// <summary>
        /// Configures the default services for testing.
        /// Override this method to customize the service configuration.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        protected virtual void ConfigureServices(IServiceCollection services)
        {
            // Logging with in-memory provider
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(LoggerProvider);
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            // Add the logger provider as a singleton for direct access
            services.AddSingleton(LoggerProvider);
            services.AddSingleton(ActivityListener);
            services.AddSingleton(MeterListener);
        }

        /// <summary>
        /// Gets the service provider, building it if necessary.
        /// </summary>
        public IServiceProvider ServiceProvider
        {
            get
            {
                if (_serviceProvider == null)
                {
                    _serviceProvider = _services.BuildServiceProvider();
                }
                return _serviceProvider;
            }
        }

        /// <summary>
        /// Gets a service from the service provider.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance.</returns>
        public T GetService<T>() where T : notnull
        {
            return ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Tries to get a service from the service provider.
        /// </summary>
        /// <typeparam name="T">The service type.</typeparam>
        /// <returns>The service instance, or null if not found.</returns>
        public T? TryGetService<T>() where T : class
        {
            return ServiceProvider.GetService<T>();
        }

        /// <summary>
        /// Creates a new scope for scoped services.
        /// </summary>
        /// <returns>A new service scope.</returns>
        public IServiceScope CreateScope()
        {
            return ServiceProvider.CreateScope();
        }

        /// <summary>
        /// Adds additional services to the fixture.
        /// Must be called before accessing the ServiceProvider.
        /// </summary>
        /// <param name="configure">Action to configure additional services.</param>
        public void ConfigureAdditionalServices(Action<IServiceCollection> configure)
        {
            if (_serviceProvider != null)
            {
                throw new InvalidOperationException(
                    "Cannot configure additional services after the service provider has been built. " +
                    "Call ConfigureAdditionalServices before accessing ServiceProvider.");
            }

            configure(_services);
        }

        /// <summary>
        /// Creates a FakeLogger for a specific category.
        /// </summary>
        /// <typeparam name="T">The type for the logger category.</typeparam>
        /// <returns>A new FakeLogger instance.</returns>
        public FakeLogger<T> CreateFakeLogger<T>()
        {
            return new FakeLogger<T>();
        }

        /// <summary>
        /// Resets all observability state for a fresh test run.
        /// </summary>
        public virtual void Reset()
        {
            LoggerProvider.Clear();
            ActivityListener.Clear();
            MeterListener.Clear();
        }

        /// <summary>
        /// Asserts that no errors were logged.
        /// </summary>
        /// <exception cref="Assertions.AssertionException">Thrown when errors were logged.</exception>
        public void AssertNoErrorsLogged()
        {
            Assertions.LogAssertions.AssertNoErrorsLogged(LoggerProvider);
        }

        /// <summary>
        /// Asserts that no error activities (spans) were recorded.
        /// </summary>
        /// <exception cref="Assertions.AssertionException">Thrown when error activities were recorded.</exception>
        public void AssertNoErrorActivities()
        {
            Assertions.ActivityAssertions.AssertNoErrorActivities(ActivityListener);
        }

        /// <summary>
        /// Gets a summary of all captured observability data.
        /// </summary>
        /// <returns>A string summary of logs, activities, and metrics.</returns>
        public string GetSummary()
        {
            var logSummary = $"Logs: {LoggerProvider.LogCount} entries";
            if (LoggerProvider.HasErrors())
            {
                logSummary += $" ({LoggerProvider.GetLogs(LogLevel.Error).Count} errors)";
            }

            var activitySummary = $"Activities: {ActivityListener.ActivityCount} spans";
            if (ActivityListener.HasErrors())
            {
                activitySummary += $" ({ActivityListener.GetErrorActivities().Count} errors)";
            }

            var metricSummary = $"Metrics: {MeterListener.MeasurementCount} measurements";

            return $"Observability Summary:\n  {logSummary}\n  {activitySummary}\n  {metricSummary}";
        }

        /// <summary>
        /// Disposes of the fixture resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the fixture resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _serviceProvider?.Dispose();
                    LoggerProvider.Dispose();
                    ActivityListener.Dispose();
                    MeterListener.Dispose();
                }
                _disposed = true;
            }
        }
    }
}

