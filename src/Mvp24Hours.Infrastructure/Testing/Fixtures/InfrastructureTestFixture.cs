//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Infrastructure.Clock;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using Mvp24Hours.Infrastructure.Testing.Http;
using System;
using System.Net.Http;

namespace Mvp24Hours.Infrastructure.Testing.Fixtures
{
    /// <summary>
    /// Test fixture for infrastructure integration tests.
    /// Provides pre-configured services and utilities for testing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This fixture provides:
    /// - Pre-configured service collection with fake implementations
    /// - Test clock for time manipulation
    /// - Test HTTP handler for mocking HTTP calls
    /// - Fake services for email, SMS, and file storage
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyIntegrationTests : IClassFixture&lt;InfrastructureTestFixture&gt;
    /// {
    ///     private readonly InfrastructureTestFixture _fixture;
    ///     
    ///     public MyIntegrationTests(InfrastructureTestFixture fixture)
    ///     {
    ///         _fixture = fixture;
    ///         _fixture.Reset(); // Reset state between tests
    ///     }
    ///     
    ///     [Fact]
    ///     public async Task MyTest()
    ///     {
    ///         var emailService = _fixture.GetService&lt;IEmailService&gt;();
    ///         // ... test code ...
    ///     }
    /// }
    /// </code>
    /// </example>
    public class InfrastructureTestFixture : IDisposable
    {
        private ServiceProvider? _serviceProvider;
        private readonly ServiceCollection _services;
        private bool _disposed;

        /// <summary>
        /// Gets the test clock for time manipulation.
        /// </summary>
        public TestClock TestClock { get; }

        /// <summary>
        /// Gets the test HTTP message handler for mocking HTTP calls.
        /// </summary>
        public TestHttpMessageHandler TestHttpHandler { get; }

        /// <summary>
        /// Gets the fake email service.
        /// </summary>
        public FakeEmailService FakeEmailService { get; }

        /// <summary>
        /// Gets the fake SMS service.
        /// </summary>
        public FakeSmsService FakeSmsService { get; }

        /// <summary>
        /// Gets the fake file storage.
        /// </summary>
        public FakeFileStorage FakeFileStorage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfrastructureTestFixture"/> class.
        /// </summary>
        public InfrastructureTestFixture()
        {
            // Initialize test components
            TestClock = new TestClock(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
            TestHttpHandler = new TestHttpMessageHandler();
            FakeEmailService = new FakeEmailService();
            FakeSmsService = new FakeSmsService();
            FakeFileStorage = new FakeFileStorage();

            // Configure services
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
            // Logging
            services.AddLogging(builder =>
            {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Clock
            services.AddSingleton<IClock>(TestClock);

            // HTTP Client
            services.AddSingleton(TestHttpHandler);
            services.AddTransient<HttpClient>(sp =>
                new HttpClient(sp.GetRequiredService<TestHttpMessageHandler>()));

            // Communication services
            services.AddSingleton<IEmailService>(FakeEmailService);
            services.AddSingleton<IFakeEmailService>(FakeEmailService);
            services.AddSingleton<ISmsService>(FakeSmsService);
            services.AddSingleton<IFakeSmsService>(FakeSmsService);

            // Storage
            services.AddSingleton<IFileStorage>(FakeFileStorage);
            services.AddSingleton<IFakeFileStorage>(FakeFileStorage);
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
        /// Resets all test state for a fresh test run.
        /// </summary>
        public virtual void Reset()
        {
            // Reset clock
            TestClock.Reset();

            // Reset HTTP handler
            TestHttpHandler.ClearRequests();
            TestHttpHandler.ClearMatchers();

            // Reset communication services
            FakeEmailService.ClearSentEmails();
            FakeEmailService.ShouldFail = false;
            FakeEmailService.SimulatedDelay = null;
            FakeEmailService.CustomResultFactory = null;

            FakeSmsService.ClearSentMessages();
            FakeSmsService.ShouldFail = false;
            FakeSmsService.SimulatedDelay = null;
            FakeSmsService.CustomResultFactory = null;

            // Reset storage
            FakeFileStorage.ClearFiles();
            FakeFileStorage.ShouldUploadFail = false;
            FakeFileStorage.ShouldDownloadFail = false;
            FakeFileStorage.SimulatedDelay = null;
            FakeFileStorage.CustomUploadResultFactory = null;
            FakeFileStorage.CustomDownloadResultFactory = null;
        }

        /// <summary>
        /// Sets the current time for the test clock.
        /// </summary>
        /// <param name="utcTime">The UTC time to set.</param>
        public void SetCurrentTime(DateTime utcTime)
        {
            TestClock.SetUtcNow(utcTime);
        }

        /// <summary>
        /// Advances the test clock by the specified duration.
        /// </summary>
        /// <param name="duration">The duration to advance.</param>
        public void AdvanceTime(TimeSpan duration)
        {
            TestClock.AdvanceBy(duration);
        }

        /// <summary>
        /// Configures the HTTP handler to return a successful response for all requests.
        /// </summary>
        /// <param name="content">Optional content to return.</param>
        public void ConfigureHttpSuccess(object? content = null)
        {
            if (content != null)
            {
                TestHttpHandler.RespondWith(System.Net.HttpStatusCode.OK, content);
            }
            else
            {
                TestHttpHandler.RespondWith(System.Net.HttpStatusCode.OK);
            }
        }

        /// <summary>
        /// Configures the HTTP handler to simulate network failure.
        /// </summary>
        public void ConfigureHttpFailure()
        {
            TestHttpHandler.SimulateNetworkFailure();
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
                }
                _disposed = true;
            }
        }
    }
}

