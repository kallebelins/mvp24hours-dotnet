//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Logging;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for configuring telemetry services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠️ DEPRECATED:</b> These extension methods are deprecated and will be removed in a future major version.
    /// </para>
    /// <para>
    /// <b>Migration Guide:</b>
    /// </para>
    /// <para>
    /// Use <c>AddLogging()</c> with <c>ILogger&lt;T&gt;</c> instead:
    /// </para>
    /// <code>
    /// // Before (deprecated):
    /// services.AddMvp24HoursTelemetry(TelemetryLevels.Information, (msg) => Console.WriteLine(msg));
    /// 
    /// // After (recommended):
    /// services.AddLogging(builder =>
    /// {
    ///     builder.AddConsole();
    ///     builder.SetMinimumLevel(LogLevel.Information);
    /// });
    /// </code>
    /// <para>
    /// For OpenTelemetry tracing and metrics:
    /// </para>
    /// <code>
    /// services.AddOpenTelemetry()
    ///     .WithTracing(builder => builder.AddSource("MyApp"))
    ///     .WithMetrics(builder => builder.AddMeter("MyApp"));
    /// </code>
    /// <para>
    /// See documentation at docs/pt-br/observability/migration.md (PT-BR) or
    /// docs/en-us/observability/migration.md (EN-US) for complete migration instructions.
    /// </para>
    /// </remarks>
    [Obsolete("Deprecated: Use AddLogging() with ILogger<T> and OpenTelemetry instead. " +
              "This class will be removed in the next major version. " +
              "See docs/observability/migration.md for migration guide.")]
    public static class TelemetryExtensions
    {
        /// <summary>
        /// Adds telemetry actions for the specified level.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with ILogger providers instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetry(this IServiceCollection services, TelemetryLevels level, params Action<string>[] actions)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.Add(level, actions);
#pragma warning restore CS0618
            return services;
        }

        /// <summary>
        /// Adds telemetry actions with arguments for the specified level.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with ILogger providers instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetry(this IServiceCollection services, TelemetryLevels level, params Action<string, object[]>[] actions)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.Add(level, actions);
#pragma warning restore CS0618
            return services;
        }

        /// <summary>
        /// Adds telemetry services for the specified level.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with ILogger providers instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetry(this IServiceCollection services, TelemetryLevels level, params ITelemetryService[] telemetryServices)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.Add(level, telemetryServices);
#pragma warning restore CS0618
            return services;
        }

        /// <summary>
        /// Adds filtered telemetry actions for a specific service.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> with log categories instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with log categories and filtering instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetryFiltered(this IServiceCollection services, string serviceName, params Action<string>[] actions)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.AddFilter(serviceName, actions);
#pragma warning restore CS0618
            return services;
        }

        /// <summary>
        /// Adds filtered telemetry actions with arguments for a specific service.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> with log categories instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with log categories and filtering instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetryFiltered(this IServiceCollection services, string serviceName, params Action<string, object[]>[] actions)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.AddFilter(serviceName, actions);
#pragma warning restore CS0618
            return services;
        }

        /// <summary>
        /// Adds filtered telemetry services for a specific service.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> with log categories instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with log categories and filtering instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetryFiltered(this IServiceCollection services, string serviceName, params ITelemetryService[] telemetryServices)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.AddFilter(serviceName, telemetryServices);
#pragma warning restore CS0618
            return services;
        }

        /// <summary>
        /// Adds service names to ignore in telemetry.
        /// </summary>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>AddLogging()</c> with log filtering instead.
        /// </remarks>
        [Obsolete("Use AddLogging() with log filtering configuration instead. Will be removed in next major version.")]
        public static IServiceCollection AddMvp24HoursTelemetryIgnore(this IServiceCollection services, params string[] ignoreServiceNames)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            TelemetryHelper.AddIgnoreService(ignoreServiceNames);
#pragma warning restore CS0618
            return services;
        }
    }
}
