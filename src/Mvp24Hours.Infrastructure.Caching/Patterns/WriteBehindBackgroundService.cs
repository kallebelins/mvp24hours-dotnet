//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Patterns
{
    /// <summary>
    /// Background service that periodically flushes pending writes from Write-Behind caches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service runs in the background and periodically processes pending writes from
    /// Write-Behind caches. It can be configured with different flush intervals and batch sizes.
    /// </para>
    /// <para>
    /// <b>.NET 6+ PeriodicTimer:</b> This implementation uses PeriodicTimer instead of
    /// Task.Delay for modern async/await patterns with proper cancellation support.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the service
    /// services.AddHostedService&lt;WriteBehindBackgroundService&gt;();
    /// 
    /// // Configure options
    /// services.Configure&lt;WriteBehindOptions&gt;(options =>
    /// {
    ///     options.FlushInterval = TimeSpan.FromSeconds(30);
    ///     options.BatchSize = 100;
    /// });
    /// </code>
    /// </example>
    public class WriteBehindBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WriteBehindOptions _options;
        private readonly ILogger<WriteBehindBackgroundService> _logger;

        /// <summary>
        /// Creates a new instance of WriteBehindBackgroundService.
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve Write-Behind caches.</param>
        /// <param name="options">The options for the background service.</param>
        /// <param name="logger">The logger.</param>
        public WriteBehindBackgroundService(
            IServiceProvider serviceProvider,
            IOptions<WriteBehindOptions> options,
            ILogger<WriteBehindBackgroundService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Write-Behind Background Service started. Flush interval: {Interval}, Batch size: {BatchSize}",
                _options.FlushInterval, _options.BatchSize);

            // Use PeriodicTimer for modern async/await patterns with proper cancellation
            using var timer = new PeriodicTimer(_options.FlushInterval);
            
            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await ProcessWriteBehindCachesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing Write-Behind caches");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Write-Behind Background Service stopping gracefully");
            }

            _logger.LogInformation("Write-Behind Background Service stopped");
        }

        private async Task ProcessWriteBehindCachesAsync(CancellationToken cancellationToken)
        {
            // Note: In a real implementation, you would need a registry of Write-Behind caches
            // For now, this is a placeholder that shows the pattern
            // You could use a service locator pattern or register caches in a collection
            
            _logger.LogDebug("Processing Write-Behind caches...");
            
            // This would iterate through registered Write-Behind caches
            // For example, if you had a IWriteBehindCacheRegistry:
            // var registry = _serviceProvider.GetService&lt;IWriteBehindCacheRegistry&gt;();
            // foreach (var cache in registry.GetAll())
            // {
            //     await cache.ProcessPendingWritesAsync(_options.BatchSize, cancellationToken);
            // }
            
            // Placeholder implementation
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Options for configuring the Write-Behind background service.
    /// </summary>
    public class WriteBehindOptions
    {
        /// <summary>
        /// Gets or sets the interval between flush operations.
        /// </summary>
        public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the maximum number of writes to process in one batch.
        /// </summary>
        public int BatchSize { get; set; } = 100;
    }
}

