//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Infrastructure.Caching.Warming;

/// <summary>
/// Implementation of cache warmer that executes warmup operations on startup.
/// </summary>
/// <remarks>
/// <para>
/// This warmer executes registered warmup operations in priority order (lowest first).
/// Operations are executed sequentially to avoid overwhelming the cache or data source.
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new instance of CacheWarmer.
/// </remarks>
/// <param name="warmupOperations">Collection of warmup operations to execute.</param>
/// <param name="logger">Optional logger.</param>
public class CacheWarmer(
    IEnumerable<ICacheWarmupOperation> warmupOperations,
    ILogger<CacheWarmer>? logger = null) : ICacheWarmer
{
    private readonly IEnumerable<ICacheWarmupOperation> _warmupOperations = warmupOperations ?? throw new ArgumentNullException(nameof(warmupOperations));
    private readonly ILogger<CacheWarmer>? _logger = logger;

    /// <inheritdoc />
    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        var operations = _warmupOperations
            .OrderBy(op => op.Priority)
            .ThenBy(op => op.Name)
            .ToList();

        if (operations.Count == 0)
        {
            _logger?.LogInformation("No cache warmup operations registered");
            return;
        }

        _logger?.LogInformation("Starting cache warmup with {Count} operations", operations.Count);

        int successCount = 0;
        int failureCount = 0;

        foreach (ICacheWarmupOperation? operation in operations)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger?.LogWarning("Cache warmup cancelled");
                break;
            }

            try
            {
                _logger?.LogDebug("Executing warmup operation: {OperationName} (Priority: {Priority})",
                    operation.Name, operation.Priority);

                DateTime startTime = DateTime.UtcNow;
                await operation.ExecuteAsync(cancellationToken);
                TimeSpan duration = DateTime.UtcNow - startTime;

                successCount++;
                _logger?.LogInformation(
                    "Warmup operation completed: {OperationName} (Duration: {Duration}ms)",
                    operation.Name, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                failureCount++;
                _logger?.LogError(ex,
                    "Warmup operation failed: {OperationName}", operation.Name);
                // Continue with other operations even if one fails
            }
        }

        _logger?.LogInformation(
            "Cache warmup completed. Success: {SuccessCount}, Failures: {FailureCount}",
            successCount, failureCount);
    }
}

