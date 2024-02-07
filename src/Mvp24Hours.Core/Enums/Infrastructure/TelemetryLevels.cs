//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Enums.Infrastructure
{
    /// <summary>
    /// Defines the level telemetry
    /// </summary>
    [System.Flags]
    public enum TelemetryLevels : short
    {
        None = 1 << 0,
        Verbose = 1 << 1,
        Information = 1 << 2,
        Warning = 1 << 3,
        Error = 1 << 4,
        Critical = 1 << 5,
        All = Verbose | Information | Warning | Error | Critical
    }
}
