using System.Diagnostics;

namespace CustomerAPI.Test.Support;

/// <summary>
/// Detects whether the Docker engine is reachable for Testcontainers-based tests.
/// </summary>
internal static class DockerAvailability
{
    private static readonly Lazy<bool> IsAvailableLazy = new(Detect, LazyThreadSafetyMode.ExecutionAndPublication);

    public static bool IsAvailable => IsAvailableLazy.Value;

    public const string SkipReason =
        "Docker is not available. Start Docker Desktop to run Testcontainers integration tests.";

    private static bool Detect()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info --format {{.ServerVersion}}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return false;
            }

            string stdout = process.StandardOutput.ReadToEnd();
            _ = process.StandardError.ReadToEnd();

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return false;
            }

            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(stdout);
        }
        catch
        {
            return false;
        }
    }
}
