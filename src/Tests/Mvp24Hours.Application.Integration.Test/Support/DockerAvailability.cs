//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;

namespace Mvp24Hours.Application.Integration.Test.Support;

/// <summary>
/// Detects whether the Docker engine is reachable for Testcontainers-based tests.
/// </summary>
internal static class DockerAvailability
{
    private static readonly Lazy<bool> IsAvailableLazy = new(Detect, LazyThreadSafetyMode.ExecutionAndPublication);

    public static bool IsAvailable => IsAvailableLazy.Value;

    public const string SkipReason =
        "Docker is not available. Start Docker Desktop to run SQL Server integration tests.";

    private static bool Detect()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
