//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Testing.Fakes;
using Mvp24Hours.Infrastructure.Testing.Http;
using Mvp24Hours.Infrastructure.Testing.Logging;
using Mvp24Hours.Infrastructure.Testing.Observability;
using System;
using System.Linq;
using System.Net.Http;

namespace Mvp24Hours.Infrastructure.Testing.Extensions
{
    /// <summary>
    /// Extension methods for registering testing services in the DI container.
    /// </summary>
    public static class TestingServiceExtensions
    {
        /// <summary>
        /// Adds all testing infrastructure services (fake implementations).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMvpTestingInfrastructure(this IServiceCollection services)
        {
            services.AddMockClock();
            services.AddFakeEmailService();
            services.AddFakeSmsService();
            services.AddFakeFileStorage();
            services.AddTestHttpHandler();

            return services;
        }

        /// <summary>
        /// Adds a MockClock as the IClock implementation for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="initialTime">Optional initial time for the clock.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMockClock(
            this IServiceCollection services,
            DateTime? initialTime = null)
        {
            var clock = initialTime.HasValue
                ? new MockClock(initialTime.Value)
                : new MockClock();

            services.AddSingleton<IClock>(clock);
            services.AddSingleton(clock);

            return services;
        }

        /// <summary>
        /// Adds a FakeEmailService as the IEmailService implementation for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFakeEmailService(
            this IServiceCollection services,
            Action<FakeEmailService>? configure = null)
        {
            var fakeService = new FakeEmailService();
            configure?.Invoke(fakeService);

            services.AddSingleton<IEmailService>(fakeService);
            services.AddSingleton<IFakeEmailService>(fakeService);
            services.AddSingleton(fakeService);

            return services;
        }

        /// <summary>
        /// Adds a FakeSmsService as the ISmsService implementation for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFakeSmsService(
            this IServiceCollection services,
            Action<FakeSmsService>? configure = null)
        {
            var fakeService = new FakeSmsService();
            configure?.Invoke(fakeService);

            services.AddSingleton<ISmsService>(fakeService);
            services.AddSingleton<IFakeSmsService>(fakeService);
            services.AddSingleton(fakeService);

            return services;
        }

        /// <summary>
        /// Adds a FakeFileStorage as the IFileStorage implementation for testing.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFakeFileStorage(
            this IServiceCollection services,
            Action<FakeFileStorage>? configure = null)
        {
            var fakeStorage = new FakeFileStorage();
            configure?.Invoke(fakeStorage);

            services.AddSingleton<IFileStorage>(fakeStorage);
            services.AddSingleton<IFakeFileStorage>(fakeStorage);
            services.AddSingleton(fakeStorage);

            return services;
        }

        /// <summary>
        /// Adds a TestHttpMessageHandler for HTTP client mocking.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddTestHttpHandler(
            this IServiceCollection services,
            Action<TestHttpMessageHandler>? configure = null)
        {
            var handler = new TestHttpMessageHandler();
            configure?.Invoke(handler);

            services.AddSingleton(handler);
            services.AddSingleton<HttpMessageHandler>(handler);

            // Add a default HttpClient using the test handler
            services.AddTransient(sp =>
            {
                var h = sp.GetRequiredService<TestHttpMessageHandler>();
                return new HttpClient(h);
            });

            return services;
        }

        /// <summary>
        /// Adds a named HttpClient using the TestHttpMessageHandler.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="name">The name of the HttpClient.</param>
        /// <param name="configureClient">Optional client configuration.</param>
        /// <param name="configureHandler">Optional handler configuration.</param>
        /// <returns>The IHttpClientBuilder for further configuration.</returns>
        public static IHttpClientBuilder AddTestHttpClient(
            this IServiceCollection services,
            string name,
            Action<HttpClient>? configureClient = null,
            Action<TestHttpMessageHandler>? configureHandler = null)
        {
            // Ensure the handler is registered
            services.AddTestHttpHandler(configureHandler);

            return services.AddHttpClient(name, client =>
            {
                configureClient?.Invoke(client);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
                sp.GetRequiredService<TestHttpMessageHandler>());
        }

        /// <summary>
        /// Adds an InMemoryLoggerProvider for capturing logs in tests.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddInMemoryLoggerProvider(
            this IServiceCollection services,
            Action<InMemoryLoggerProvider>? configure = null)
        {
            var provider = new InMemoryLoggerProvider();
            configure?.Invoke(provider);

            services.AddSingleton(provider);
            services.AddSingleton<ILoggerProvider>(provider);

            // Configure logging to use the in-memory provider
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(provider);
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            return services;
        }

        /// <summary>
        /// Adds a FakeActivityListener for capturing distributed tracing spans in tests.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="sourceFilter">Optional filter for activity source names. Supports wildcard (*) at the end.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFakeActivityListener(
            this IServiceCollection services,
            string? sourceFilter = null)
        {
            var listener = new FakeActivityListener(sourceFilter);
            services.AddSingleton(listener);

            return services;
        }

        /// <summary>
        /// Adds a FakeMeterListener for capturing metrics in tests.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="meterFilter">Optional filter for meter names. Supports wildcard (*) at the end.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddFakeMeterListener(
            this IServiceCollection services,
            string? meterFilter = null)
        {
            var listener = new FakeMeterListener(meterFilter);
            services.AddSingleton(listener);

            return services;
        }

        /// <summary>
        /// Adds all observability testing infrastructure (logging, tracing, metrics).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="sourceFilter">Optional filter for source/meter names. Supports wildcard (*) at the end.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddObservabilityTesting(
            this IServiceCollection services,
            string? sourceFilter = null)
        {
            services.AddInMemoryLoggerProvider();
            services.AddFakeActivityListener(sourceFilter);
            services.AddFakeMeterListener(sourceFilter);

            return services;
        }

        /// <summary>
        /// Replaces all infrastructure services with test implementations.
        /// Use this in integration tests to isolate from external dependencies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration for the test services.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ReplaceWithTestInfrastructure(
            this IServiceCollection services,
            Action<TestInfrastructureOptions>? configure = null)
        {
            var options = new TestInfrastructureOptions();
            configure?.Invoke(options);

            // Remove existing registrations
            services.RemoveAll<IEmailService>();
            services.RemoveAll<ISmsService>();
            services.RemoveAll<IFileStorage>();
            services.RemoveAll<IClock>();
            services.RemoveAll<HttpClient>();

            // Add test implementations
            if (options.UseMockClock)
            {
                services.AddMockClock(options.InitialClockTime);
            }

            if (options.UseFakeEmail)
            {
                services.AddFakeEmailService(email =>
                {
                    if (options.EmailShouldFail)
                    {
                        email.ShouldFail = true;
                    }
                });
            }

            if (options.UseFakeSms)
            {
                services.AddFakeSmsService(sms =>
                {
                    if (options.SmsShouldFail)
                    {
                        sms.ShouldFail = true;
                    }
                });
            }

            if (options.UseFakeFileStorage)
            {
                services.AddFakeFileStorage();
            }

            if (options.UseFakeHttp)
            {
                services.AddTestHttpHandler();
            }

            return services;
        }

        private static void RemoveAll<T>(this IServiceCollection services)
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }
        }
    }

    /// <summary>
    /// Options for configuring test infrastructure replacements.
    /// </summary>
    public class TestInfrastructureOptions
    {
        /// <summary>
        /// Gets or sets whether to use a mock clock. Default is true.
        /// </summary>
        public bool UseMockClock { get; set; } = true;

        /// <summary>
        /// Gets or sets the initial time for the mock clock.
        /// </summary>
        public DateTime? InitialClockTime { get; set; }

        /// <summary>
        /// Gets or sets whether to use fake email service. Default is true.
        /// </summary>
        public bool UseFakeEmail { get; set; } = true;

        /// <summary>
        /// Gets or sets whether email service should simulate failures.
        /// </summary>
        public bool EmailShouldFail { get; set; }

        /// <summary>
        /// Gets or sets whether to use fake SMS service. Default is true.
        /// </summary>
        public bool UseFakeSms { get; set; } = true;

        /// <summary>
        /// Gets or sets whether SMS service should simulate failures.
        /// </summary>
        public bool SmsShouldFail { get; set; }

        /// <summary>
        /// Gets or sets whether to use fake file storage. Default is true.
        /// </summary>
        public bool UseFakeFileStorage { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use fake HTTP handler. Default is true.
        /// </summary>
        public bool UseFakeHttp { get; set; } = true;
    }
}

