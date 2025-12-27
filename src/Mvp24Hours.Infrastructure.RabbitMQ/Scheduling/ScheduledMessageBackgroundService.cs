//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Scheduling
{
    /// <summary>
    /// Background service that periodically processes scheduled messages.
    /// </summary>
    public class ScheduledMessageBackgroundService : BackgroundService
    {
        private readonly MessageScheduler _scheduler;
        private readonly IScheduledMessageStore _store;
        private readonly MessageSchedulerOptions _options;
        private readonly ILogger<ScheduledMessageBackgroundService>? _logger;
        private bool _isStarted;

        /// <summary>
        /// Initializes a new instance of the ScheduledMessageBackgroundService.
        /// </summary>
        /// <param name="scheduler">The message scheduler.</param>
        /// <param name="store">The scheduled message store.</param>
        /// <param name="options">The scheduler options.</param>
        /// <param name="logger">Optional logger.</param>
        public ScheduledMessageBackgroundService(
            MessageScheduler scheduler,
            IScheduledMessageStore store,
            IOptions<MessageSchedulerOptions> options,
            ILogger<ScheduledMessageBackgroundService>? logger = null)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options?.Value ?? new MessageSchedulerOptions();
            _logger = logger;
        }

        /// <inheritdoc />
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.Enabled)
            {
                _logger?.LogInformation("Message scheduler is disabled");
                return;
            }

            _isStarted = true;
            _logger?.LogInformation(
                "Starting scheduled message background service with polling interval {PollingInterval}",
                _options.PollingInterval);

            await base.StartAsync(cancellationToken);
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_isStarted)
            {
                return;
            }

            _logger?.LogDebug("Scheduled message background service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = await _scheduler.ProcessDueMessagesAsync(stoppingToken);

                    if (processed > 0)
                    {
                        _logger?.LogDebug("Processed {Count} scheduled messages", processed);
                    }

                    // Cleanup old messages periodically
                    await CleanupOldMessagesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing scheduled messages");
                }

                try
                {
                    await Task.Delay(_options.PollingInterval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger?.LogInformation("Scheduled message background service stopped");
        }

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isStarted)
            {
                return;
            }

            _logger?.LogInformation("Stopping scheduled message background service");

            await base.StopAsync(cancellationToken);
        }

        private DateTime _lastCleanup = DateTime.MinValue;

        private async Task CleanupOldMessagesAsync(CancellationToken cancellationToken)
        {
            // Cleanup every hour
            if ((DateTime.UtcNow - _lastCleanup).TotalHours < 1)
            {
                return;
            }

            try
            {
                var olderThan = DateTimeOffset.UtcNow.Subtract(_options.CompletedMessageTtl);
                var removed = await _store.CleanupOldMessagesAsync(olderThan, cancellationToken);

                if (removed > 0)
                {
                    _logger?.LogInformation("Cleaned up {Count} old scheduled messages", removed);
                }

                _lastCleanup = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error cleaning up old scheduled messages");
            }
        }
    }
}

