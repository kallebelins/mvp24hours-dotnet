//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Logging
{
    /// <summary>
    /// Interface for telemetry service implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠️ DEPRECATED:</b> This interface is deprecated and will be removed in a future major version.
    /// </para>
    /// <para>
    /// <b>Migration Guide:</b>
    /// </para>
    /// <para>
    /// Use <c>ILogger&lt;T&gt;</c> (Microsoft.Extensions.Logging) instead:
    /// </para>
    /// <code>
    /// // Before (deprecated):
    /// public class MyTelemetryService : ITelemetryService
    /// {
    ///     public void Execute(string eventName, params object[] args)
    ///     {
    ///         Console.WriteLine($"{eventName}: {string.Join(", ", args)}");
    ///     }
    /// }
    /// 
    /// // After (recommended):
    /// public class MyService
    /// {
    ///     private readonly ILogger&lt;MyService&gt; _logger;
    ///     
    ///     public MyService(ILogger&lt;MyService&gt; logger)
    ///     {
    ///         _logger = logger;
    ///     }
    ///     
    ///     public void DoSomething()
    ///     {
    ///         _logger.LogInformation("Event: {Arg1}, {Arg2}", arg1, arg2);
    ///     }
    /// }
    /// </code>
    /// <para>
    /// For custom logging providers, implement <c>ILoggerProvider</c> instead.
    /// </para>
    /// <para>
    /// See documentation at docs/pt-br/observability/migration.md (PT-BR) or
    /// docs/en-us/observability/migration.md (EN-US) for complete migration instructions.
    /// </para>
    /// </remarks>
    [Obsolete("Deprecated: Use ILogger<T> (Microsoft.Extensions.Logging) instead. " +
              "This interface will be removed in the next major version. " +
              "See docs/observability/migration.md for migration guide.")]
    public interface ITelemetryService
    {
        /// <summary>
        /// Executes telemetry logging for the specified event.
        /// </summary>
        /// <param name="eventName">The name of the event to log.</param>
        /// <param name="args">Additional arguments to include in the log.</param>
        /// <remarks>
        /// <b>⚠️ DEPRECATED:</b> Use <c>ILogger.Log*()</c> methods instead.
        /// </remarks>
        void Execute(string eventName, params object[] args);
    }
}
