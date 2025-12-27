//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Enums.Infrastructure
{
    /// <summary>
    /// Defines the telemetry level for logging operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>⚠️ DEPRECATED:</b> This enum is deprecated and will be removed in a future major version.
    /// </para>
    /// <para>
    /// <b>Migration Guide:</b>
    /// </para>
    /// <para>
    /// Use <c>Microsoft.Extensions.Logging.LogLevel</c> instead:
    /// </para>
    /// <list type="table">
    /// <listheader>
    /// <term>TelemetryLevels (Old)</term>
    /// <description>LogLevel (New)</description>
    /// </listheader>
    /// <item><term>Verbose</term><description>LogLevel.Debug or LogLevel.Trace</description></item>
    /// <item><term>Information</term><description>LogLevel.Information</description></item>
    /// <item><term>Warning</term><description>LogLevel.Warning</description></item>
    /// <item><term>Error</term><description>LogLevel.Error</description></item>
    /// <item><term>Critical</term><description>LogLevel.Critical</description></item>
    /// </list>
    /// <para>
    /// See documentation at docs/pt-br/observability/migration.md (PT-BR) or
    /// docs/en-us/observability/migration.md (EN-US) for complete migration instructions.
    /// </para>
    /// </remarks>
    [Flags]
    [Obsolete("Deprecated: Use Microsoft.Extensions.Logging.LogLevel instead. " +
              "This enum will be removed in the next major version. " +
              "See docs/observability/migration.md for migration guide.")]
    public enum TelemetryLevels : short
    {
        /// <summary>No telemetry.</summary>
        None = 1 << 0,
        /// <summary>Verbose/Debug level. Use LogLevel.Debug or LogLevel.Trace instead.</summary>
        Verbose = 1 << 1,
        /// <summary>Information level. Use LogLevel.Information instead.</summary>
        Information = 1 << 2,
        /// <summary>Warning level. Use LogLevel.Warning instead.</summary>
        Warning = 1 << 3,
        /// <summary>Error level. Use LogLevel.Error instead.</summary>
        Error = 1 << 4,
        /// <summary>Critical level. Use LogLevel.Critical instead.</summary>
        Critical = 1 << 5,
        /// <summary>All levels. Configure log level filtering via ILoggingBuilder instead.</summary>
        All = Verbose | Information | Warning | Error | Critical
    }
}
