//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Infrastructure.Caching
{
    /// <summary>
    /// Interface for warming up cache on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cache warming preloads frequently accessed data into cache during application startup,
    /// reducing cold start latency and improving initial response times. This is especially
    /// useful for:
    /// <list type="bullet">
    /// <item>Reference data (e.g., configuration, lookup tables)</item>
    /// <item>Frequently accessed data (e.g., popular products, user profiles)</item>
    /// <item>Data that takes time to compute (e.g., aggregations, reports)</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface ICacheWarmer
    {
        /// <summary>
        /// Warms up the cache by executing all registered warmup operations.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the warmup operation.</returns>
        Task WarmUpAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for cache warmup operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface to define warmup operations that will be executed
    /// during application startup to preload cache with frequently accessed data.
    /// </para>
    /// </remarks>
    public interface ICacheWarmupOperation
    {
        /// <summary>
        /// Gets the name of the warmup operation (for logging/identification).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority of this warmup operation (lower numbers execute first).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Executes the warmup operation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the warmup operation.</returns>
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}

