//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Warming
{
    /// <summary>
    /// Hosted service that executes cache warmup on application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This hosted service automatically executes cache warmup operations when the application starts.
    /// It runs after the application has fully initialized but before accepting requests.
    /// </para>
    /// </remarks>
    public class CacheWarmupHostedService : IHostedService
    {
        private readonly ICacheWarmer _cacheWarmer;
        private readonly ILogger<CacheWarmupHostedService>? _logger;

        /// <summary>
        /// Creates a new instance of CacheWarmupHostedService.
        /// </summary>
        /// <param name="cacheWarmer">The cache warmer to use.</param>
        /// <param name="logger">Optional logger.</param>
        public CacheWarmupHostedService(
            ICacheWarmer cacheWarmer,
            ILogger<CacheWarmupHostedService>? logger = null)
        {
            _cacheWarmer = cacheWarmer ?? throw new ArgumentNullException(nameof(cacheWarmer));
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Starting cache warmup hosted service");
            
            try
            {
                await _cacheWarmer.WarmUpAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during cache warmup");
                // Don't throw - warmup failures shouldn't prevent application startup
            }
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Stopping cache warmup hosted service");
            return Task.CompletedTask;
        }
    }
}

