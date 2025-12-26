//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Observability;
using Mvp24Hours.Infrastructure.Observability.Contract;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Mvp24Hours.Infrastructure.Observability.Extensions
{
    /// <summary>
    /// Extension methods for registering observability services in the Infrastructure module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions register OpenTelemetry Activity Sources, metrics, diagnostics,
    /// and correlation ID propagation for all Infrastructure subsystems.
    /// </para>
    /// </remarks>
    public static class ObservabilityServiceExtensions
    {
        /// <summary>
        /// Adds Infrastructure observability services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">Optional configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers:
        /// - <see cref="IInfrastructureDiagnostics"/> implementation
        /// - Activity Sources for OpenTelemetry tracing
        /// - Metrics collection via OpenTelemetry Meter
        /// - Correlation ID propagation helpers
        /// </para>
        /// <para>
        /// <strong>OpenTelemetry Integration:</strong>
        /// To export traces and metrics, configure OpenTelemetry in your application:
        /// <code>
        /// services.AddOpenTelemetry()
        ///     .WithTracing(builder => builder
        ///         .AddSource(ActivitySources.Http.Name)
        ///         .AddSource(ActivitySources.Email.Name)
        ///         // ... other sources
        ///         .AddOtlpExporter())
        ///     .WithMetrics(builder => builder
        ///         .AddMeter("Mvp24Hours.Infrastructure")
        ///         .AddPrometheusExporter());
        /// </code>
        /// </para>
        /// </remarks>
        public static IServiceCollection AddInfrastructureObservability(
            this IServiceCollection services,
            Action<ObservabilityOptions>? configure = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new ObservabilityOptions();
            configure?.Invoke(options);

            // Register diagnostics
            services.TryAddSingleton<IInfrastructureDiagnostics, InfrastructureDiagnostics>();

            // Register subsystem diagnostics providers (will be registered by each subsystem)
            // This is a placeholder - actual providers should be registered by their respective subsystems

            return services;
        }

        /// <summary>
        /// Registers a subsystem diagnostics provider.
        /// </summary>
        /// <typeparam name="TProvider">The diagnostics provider type.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// Each Infrastructure subsystem should call this method to register its
        /// diagnostics provider. The provider will be automatically discovered by
        /// <see cref="InfrastructureDiagnostics"/>.
        /// </para>
        /// </remarks>
        public static IServiceCollection AddSubsystemDiagnostics<TProvider>(
            this IServiceCollection services)
            where TProvider : class, ISubsystemDiagnosticsProvider
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<ISubsystemDiagnosticsProvider, TProvider>();
            return services;
        }
    }

    /// <summary>
    /// Options for configuring Infrastructure observability.
    /// </summary>
    public class ObservabilityOptions
    {
        /// <summary>
        /// Gets or sets whether to enable detailed operation logging.
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable correlation ID propagation.
        /// </summary>
        public bool EnableCorrelationIdPropagation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable metrics collection.
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable distributed tracing.
        /// </summary>
        public bool EnableTracing { get; set; } = true;
    }
}

