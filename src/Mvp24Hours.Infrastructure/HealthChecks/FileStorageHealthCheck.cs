//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check for file storage providers to verify connectivity and write/read operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies file storage provider health by:
    /// <list type="bullet">
    /// <item>Uploading a test file</item>
    /// <item>Verifying the file exists</item>
    /// <item>Downloading and verifying file content</item>
    /// <item>Deleting the test file</item>
    /// <item>Measuring operation response times</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddFileStorageHealthCheck(
    ///         "file-storage",
    ///         options =>
    ///         {
    ///             options.TestFilePath = "health-check/test.txt";
    ///             options.TimeoutSeconds = 10;
    ///         });
    /// </code>
    /// </example>
    public class FileStorageHealthCheck : IHealthCheck
    {
        private readonly IFileStorage _fileStorage;
        private readonly FileStorageHealthCheckOptions _options;
        private readonly ILogger<FileStorageHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileStorageHealthCheck"/> class.
        /// </summary>
        /// <param name="fileStorage">The file storage provider to check.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public FileStorageHealthCheck(
            IFileStorage fileStorage,
            FileStorageHealthCheckOptions? options,
            ILogger<FileStorageHealthCheck> logger)
        {
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _options = options ?? new FileStorageHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();
            var testFilePath = _options.TestFilePath ?? $"health-check/{Guid.NewGuid():N}.txt";
            var testContent = Encoding.UTF8.GetBytes(_options.TestContent);

            try
            {
                data["testFilePath"] = testFilePath;
                data["testContentSize"] = testContent.Length;

                // Create timeout cancellation token
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                // Step 1: Upload test file
                var uploadStopwatch = Stopwatch.StartNew();
                var uploadResult = await _fileStorage.UploadAsync(
                    testFilePath,
                    testContent,
                    "text/plain",
                    null,
                    cts.Token);

                uploadStopwatch.Stop();
                data["uploadTimeMs"] = uploadStopwatch.ElapsedMilliseconds;

                if (!uploadResult.Success)
                {
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["error"] = uploadResult.ErrorMessage ?? "Upload failed";

                    _logger.LogError("File storage health check failed: Upload failed. Error: {Error}", uploadResult.ErrorMessage);

                    return HealthCheckResult.Unhealthy(
                        description: $"File storage upload failed: {uploadResult.ErrorMessage}",
                        data: data);
                }

                data["uploadedFilePath"] = uploadResult.FilePath;

                // Step 2: Verify file exists
                var existsStopwatch = Stopwatch.StartNew();
                var exists = await _fileStorage.ExistsAsync(testFilePath, cts.Token);
                existsStopwatch.Stop();
                data["existsCheckTimeMs"] = existsStopwatch.ElapsedMilliseconds;

                if (!exists)
                {
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["error"] = "File does not exist after upload";

                    _logger.LogError("File storage health check failed: File does not exist after upload");

                    // Try to clean up
                    try
                    {
                        await _fileStorage.DeleteAsync(testFilePath, cts.Token);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }

                    return HealthCheckResult.Unhealthy(
                        description: "File storage: File does not exist after upload",
                        data: data);
                }

                // Step 3: Download and verify content
                var downloadStopwatch = Stopwatch.StartNew();
                var downloadResult = await _fileStorage.DownloadAsync(testFilePath, cts.Token);
                downloadStopwatch.Stop();
                data["downloadTimeMs"] = downloadStopwatch.ElapsedMilliseconds;

                if (!downloadResult.Success)
                {
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["error"] = downloadResult.ErrorMessage ?? "Download failed";

                    _logger.LogError("File storage health check failed: Download failed. Error: {Error}", downloadResult.ErrorMessage);

                    // Try to clean up
                    try
                    {
                        await _fileStorage.DeleteAsync(testFilePath, cts.Token);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }

                    return HealthCheckResult.Unhealthy(
                        description: $"File storage download failed: {downloadResult.ErrorMessage}",
                        data: data);
                }

                // Verify content matches
                if (downloadResult.Content == null || downloadResult.Content.Length != testContent.Length)
                {
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["error"] = "Downloaded content size does not match";

                    _logger.LogError("File storage health check failed: Content size mismatch. Expected: {Expected}, Actual: {Actual}",
                        testContent.Length, downloadResult.Content?.Length ?? 0);

                    // Try to clean up
                    try
                    {
                        await _fileStorage.DeleteAsync(testFilePath, cts.Token);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }

                    return HealthCheckResult.Unhealthy(
                        description: "File storage: Downloaded content size does not match",
                        data: data);
                }

                // Verify content bytes match
                if (!_options.SkipContentVerification)
                {
                    for (int i = 0; i < testContent.Length; i++)
                    {
                        if (downloadResult.Content[i] != testContent[i])
                        {
                            stopwatch.Stop();
                            data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                            data["error"] = "Downloaded content does not match";

                            _logger.LogError("File storage health check failed: Content mismatch at byte {Index}", i);

                            // Try to clean up
                            try
                            {
                                await _fileStorage.DeleteAsync(testFilePath, cts.Token);
                            }
                            catch
                            {
                                // Ignore cleanup errors
                            }

                            return HealthCheckResult.Unhealthy(
                                description: "File storage: Downloaded content does not match",
                                data: data);
                        }
                    }
                }

                // Step 4: Delete test file
                var deleteStopwatch = Stopwatch.StartNew();
                var deleted = await _fileStorage.DeleteAsync(testFilePath, cts.Token);
                deleteStopwatch.Stop();
                data["deleteTimeMs"] = deleteStopwatch.ElapsedMilliseconds;
                data["deleted"] = deleted;

                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["totalOperations"] = 4; // Upload, Exists, Download, Delete

                // Check response time thresholds
                if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                {
                    return HealthCheckResult.Unhealthy(
                        description: $"File storage response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                        data: data);
                }

                if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                {
                    return HealthCheckResult.Degraded(
                        description: $"File storage response time {stopwatch.ElapsedMilliseconds}ms is slow",
                        data: data);
                }

                return HealthCheckResult.Healthy(
                    description: $"File storage is healthy (total time: {stopwatch.ElapsedMilliseconds}ms)",
                    data: data);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = "Operation timeout";

                _logger.LogWarning("File storage health check timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);

                // Try to clean up
                try
                {
                    await _fileStorage.DeleteAsync(testFilePath, CancellationToken.None);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return HealthCheckResult.Unhealthy(
                    description: $"File storage health check timed out after {_options.TimeoutSeconds}s",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "File storage health check failed with unexpected error");

                // Try to clean up
                try
                {
                    await _fileStorage.DeleteAsync(testFilePath, CancellationToken.None);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                return HealthCheckResult.Unhealthy(
                    description: $"File storage health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }
    }

    /// <summary>
    /// Configuration options for file storage health checks.
    /// </summary>
    public sealed class FileStorageHealthCheckOptions
    {
        /// <summary>
        /// Test file path to use for health check. If null, a unique path is generated.
        /// Default is null (auto-generated).
        /// </summary>
        public string? TestFilePath { get; set; }

        /// <summary>
        /// Test file content to upload and verify.
        /// Default is "Health check test content".
        /// </summary>
        public string TestContent { get; set; } = "Health check test content";

        /// <summary>
        /// Timeout in seconds for the health check operations.
        /// Default is 10 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Whether to skip byte-by-byte content verification.
        /// Default is false (verifies content).
        /// </summary>
        public bool SkipContentVerification { get; set; }

        /// <summary>
        /// Response time threshold in milliseconds for degraded status.
        /// Default is 1000ms.
        /// </summary>
        public int DegradedThresholdMs { get; set; } = 1000;

        /// <summary>
        /// Response time threshold in milliseconds for unhealthy status.
        /// Default is 5000ms.
        /// </summary>
        public int FailureThresholdMs { get; set; } = 5000;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "file-storage", "storage", "ready" };
    }
}

