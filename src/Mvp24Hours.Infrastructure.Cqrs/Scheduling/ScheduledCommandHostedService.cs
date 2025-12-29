//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// Background service that processes scheduled commands.
    /// Uses .NET 8+ TimeProvider abstraction for testable time operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service uses TimeProvider for all time-related operations, enabling:
    /// - Deterministic testing with FakeTimeProvider
    /// - Time manipulation in integration tests
    /// - Consistent time across the application
    /// </para>
    /// <para>
    /// <b>.NET 6+ PeriodicTimer:</b> This implementation uses PeriodicTimer instead of
    /// Task.Delay for modern async/await patterns with proper cancellation support.
    /// </para>
    /// </remarks>
    public class ScheduledCommandHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScheduledCommandHostedService> _logger;
        private readonly ScheduledCommandOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="ScheduledCommandHostedService"/>.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory</param>
        /// <param name="logger">The logger</param>
        /// <param name="options">The scheduling options</param>
        /// <param name="timeProvider">
        /// Optional TimeProvider for time abstraction. Defaults to TimeProvider.System.
        /// Inject FakeTimeProvider for testing.
        /// </param>
        public ScheduledCommandHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<ScheduledCommandHostedService> logger,
            ScheduledCommandOptions? options = null,
            TimeProvider? timeProvider = null)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new ScheduledCommandOptions();
            _timeProvider = timeProvider ?? TimeProvider.System;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduled command processor started with interval {Interval}ms",
                _options.PollingInterval.TotalMilliseconds);

            // Use PeriodicTimer for modern async/await patterns with proper cancellation
            using var timer = new PeriodicTimer(_options.PollingInterval);
            
            try
            {
                // Process immediately on startup, then periodically
                await ProcessAllAsync(stoppingToken);
                
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        await ProcessAllAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error processing scheduled commands");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Scheduled command processor stopping gracefully");
            }

            _logger.LogInformation("Scheduled command processor stopped");
        }

        private async Task ProcessAllAsync(CancellationToken stoppingToken)
        {
            await ProcessScheduledCommandsAsync(stoppingToken);
            await ProcessRetryCommandsAsync(stoppingToken);
            await MarkExpiredCommandsAsync(stoppingToken);

            if (_options.PurgeCompletedAfter.HasValue)
            {
                await PurgeOldCommandsAsync(stoppingToken);
            }
        }

        private async Task ProcessScheduledCommandsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IScheduledCommandStore>();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var commands = await store.GetReadyForExecutionAsync(_options.BatchSize, cancellationToken);

            foreach (var entry in commands)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await ProcessCommandAsync(entry, store, mediator, cancellationToken);
            }
        }

        private async Task ProcessRetryCommandsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IScheduledCommandStore>();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

            var commands = await store.GetReadyForRetryAsync(_options.BatchSize, cancellationToken);

            foreach (var entry in commands)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await ProcessCommandAsync(entry, store, mediator, cancellationToken);
            }
        }

        private async Task ProcessCommandAsync(
            ScheduledCommandEntry entry,
            IScheduledCommandStore store,
            ISender mediator,
            CancellationToken cancellationToken)
        {
            try
            {
                // Check if expired
                if (entry.IsExpired)
                {
                    entry.Status = ScheduledCommandStatus.Expired;
                    await store.UpdateAsync(entry, cancellationToken);
                    _logger.LogWarning("Command {CommandId} expired before execution", entry.Id);
                    return;
                }

                // Mark as processing
                entry.Status = ScheduledCommandStatus.Processing;
                entry.ProcessedAt = _timeProvider.GetUtcNow().UtcDateTime;
                await store.UpdateAsync(entry, cancellationToken);

                _logger.LogDebug("Processing scheduled command {CommandId} of type {CommandType}",
                    entry.Id, entry.CommandType);

                // Deserialize and execute
                var commandType = Type.GetType(entry.CommandType);
                if (commandType == null)
                {
                    throw new InvalidOperationException($"Command type not found: {entry.CommandType}");
                }

                var command = JsonSerializer.Deserialize(entry.CommandPayload, commandType, _jsonOptions);
                if (command == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize command: {entry.Id}");
                }

                // Execute via mediator
                await mediator.SendAsync((dynamic)command, cancellationToken);

                // Mark as completed
                entry.Status = ScheduledCommandStatus.Completed;
                entry.CompletedAt = _timeProvider.GetUtcNow().UtcDateTime;
                entry.ErrorMessage = null;
                await store.UpdateAsync(entry, cancellationToken);

                _logger.LogInformation("Scheduled command {CommandId} completed successfully", entry.Id);
            }
            catch (Exception ex)
            {
                entry.RetryCount++;
                entry.ErrorMessage = ex.Message;

                if (entry.RetryCount >= entry.MaxRetries)
                {
                    entry.Status = ScheduledCommandStatus.Failed;
                    _logger.LogError(ex,
                        "Scheduled command {CommandId} failed after {RetryCount} attempts",
                        entry.Id, entry.RetryCount);
                }
                else
                {
                    entry.Status = ScheduledCommandStatus.Failed;
                    entry.NextRetryAt = CalculateNextRetryTime(entry.RetryCount);
                    _logger.LogWarning(ex,
                        "Scheduled command {CommandId} failed (attempt {RetryCount}/{MaxRetries}), next retry at {NextRetryAt}",
                        entry.Id, entry.RetryCount, entry.MaxRetries, entry.NextRetryAt);
                }

                await store.UpdateAsync(entry, cancellationToken);
            }
        }

        private DateTime CalculateNextRetryTime(int retryCount)
        {
            // Exponential backoff: 5s, 25s, 125s, 625s, etc.
            var baseDelay = _options.RetryBaseDelay;
            var delay = TimeSpan.FromSeconds(baseDelay.TotalSeconds * Math.Pow(5, retryCount - 1));

            // Cap at max delay
            if (delay > _options.RetryMaxDelay)
            {
                delay = _options.RetryMaxDelay;
            }

            return _timeProvider.GetUtcNow().UtcDateTime.Add(delay);
        }

        private async Task MarkExpiredCommandsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IScheduledCommandStore>();

            var count = await store.MarkExpiredAsync(cancellationToken);
            if (count > 0)
            {
                _logger.LogInformation("Marked {Count} commands as expired", count);
            }
        }

        private async Task PurgeOldCommandsAsync(CancellationToken cancellationToken)
        {
            if (!_options.PurgeCompletedAfter.HasValue) return;

            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IScheduledCommandStore>();

            var olderThan = _timeProvider.GetUtcNow().UtcDateTime.Subtract(_options.PurgeCompletedAfter.Value);
            var count = await store.PurgeCompletedAsync(olderThan, cancellationToken);

            if (count > 0)
            {
                _logger.LogInformation("Purged {Count} completed commands older than {OlderThan}",
                    count, olderThan);
            }
        }
    }

    /// <summary>
    /// Options for the scheduled command processor.
    /// </summary>
    public class ScheduledCommandOptions
    {
        /// <summary>
        /// Gets or sets the polling interval for checking new commands.
        /// Default: 5 seconds.
        /// </summary>
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the batch size for processing commands.
        /// Default: 100.
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets or sets the base delay for retry calculations.
        /// Default: 5 seconds.
        /// </summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// Default: 1 hour.
        /// </summary>
        public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the time after which completed commands are purged.
        /// Set to null to disable purging. Default: 7 days.
        /// </summary>
        public TimeSpan? PurgeCompletedAfter { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets whether to enable the processor.
        /// Default: true.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}

