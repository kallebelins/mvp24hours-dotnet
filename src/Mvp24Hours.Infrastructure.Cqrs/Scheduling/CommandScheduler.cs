//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Cqrs.Scheduling
{
    /// <summary>
    /// Default implementation of <see cref="ICommandScheduler"/>.
    /// </summary>
    public class CommandScheduler : ICommandScheduler
    {
        private readonly IScheduledCommandStore _store;
        private readonly ILogger<CommandScheduler> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Creates a new instance of <see cref="CommandScheduler"/>.
        /// </summary>
        /// <param name="store">The scheduled command store</param>
        /// <param name="logger">The logger</param>
        public CommandScheduler(
            IScheduledCommandStore store,
            ILogger<CommandScheduler> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public async Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            ScheduleOptions options,
            CancellationToken cancellationToken = default)
            where TCommand : class
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (options == null) throw new ArgumentNullException(nameof(options));

            var commandType = typeof(TCommand);
            var entry = new ScheduledCommandEntry
            {
                CommandType = $"{commandType.FullName}, {commandType.Assembly.GetName().Name}",
                CommandPayload = JsonSerializer.Serialize(command, _jsonOptions),
                ScheduledAt = options.ScheduledAt,
                MaxRetries = options.MaxRetries,
                ExpiresAt = options.ExpiresAt,
                Priority = options.Priority,
                CorrelationId = options.CorrelationId,
                Metadata = options.Metadata != null
                    ? JsonSerializer.Serialize(options.Metadata, _jsonOptions)
                    : null
            };

            await _store.SaveAsync(entry, cancellationToken);

            _logger.LogInformation(
                "Command {CommandType} scheduled with ID {CommandId} for {ScheduledAt}",
                commandType.Name,
                entry.Id,
                options.ScheduledAt);

            return entry.Id;
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class
        {
            return ScheduleAsync(command, ScheduleOptions.Now(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            TimeSpan delay,
            CancellationToken cancellationToken = default)
            where TCommand : class
        {
            return ScheduleAsync(command, ScheduleOptions.After(delay), cancellationToken);
        }

        /// <inheritdoc />
        public Task<string> ScheduleAsync<TCommand>(
            TCommand command,
            DateTime scheduledAt,
            CancellationToken cancellationToken = default)
            where TCommand : class
        {
            return ScheduleAsync(command, ScheduleOptions.At(scheduledAt), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> CancelAsync(string commandId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(commandId)) return false;

            var entry = await _store.GetByIdAsync(commandId, cancellationToken);
            if (entry == null)
            {
                _logger.LogWarning("Scheduled command {CommandId} not found for cancellation", commandId);
                return false;
            }

            if (entry.Status != ScheduledCommandStatus.Pending)
            {
                _logger.LogWarning(
                    "Cannot cancel command {CommandId} with status {Status}",
                    commandId,
                    entry.Status);
                return false;
            }

            entry.Status = ScheduledCommandStatus.Cancelled;
            await _store.UpdateAsync(entry, cancellationToken);

            _logger.LogInformation("Scheduled command {CommandId} cancelled", commandId);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RescheduleAsync(
            string commandId,
            DateTime newScheduledAt,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(commandId)) return false;

            var entry = await _store.GetByIdAsync(commandId, cancellationToken);
            if (entry == null)
            {
                _logger.LogWarning("Scheduled command {CommandId} not found for rescheduling", commandId);
                return false;
            }

            if (entry.Status != ScheduledCommandStatus.Pending &&
                entry.Status != ScheduledCommandStatus.Failed)
            {
                _logger.LogWarning(
                    "Cannot reschedule command {CommandId} with status {Status}",
                    commandId,
                    entry.Status);
                return false;
            }

            var oldScheduledAt = entry.ScheduledAt;
            entry.ScheduledAt = newScheduledAt;
            entry.Status = ScheduledCommandStatus.Pending;
            entry.NextRetryAt = null;

            await _store.UpdateAsync(entry, cancellationToken);

            _logger.LogInformation(
                "Scheduled command {CommandId} rescheduled from {OldTime} to {NewTime}",
                commandId,
                oldScheduledAt,
                newScheduledAt);

            return true;
        }

        /// <inheritdoc />
        public Task<ScheduledCommandEntry?> GetStatusAsync(
            string commandId,
            CancellationToken cancellationToken = default)
        {
            return _store.GetByIdAsync(commandId, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<bool> RetryAsync(string commandId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(commandId)) return false;

            var entry = await _store.GetByIdAsync(commandId, cancellationToken);
            if (entry == null)
            {
                _logger.LogWarning("Scheduled command {CommandId} not found for retry", commandId);
                return false;
            }

            if (!entry.CanRetry)
            {
                _logger.LogWarning(
                    "Cannot retry command {CommandId} - Status: {Status}, RetryCount: {RetryCount}/{MaxRetries}",
                    commandId,
                    entry.Status,
                    entry.RetryCount,
                    entry.MaxRetries);
                return false;
            }

            entry.Status = ScheduledCommandStatus.Pending;
            entry.ScheduledAt = DateTime.UtcNow;
            entry.NextRetryAt = null;

            await _store.UpdateAsync(entry, cancellationToken);

            _logger.LogInformation(
                "Scheduled command {CommandId} queued for retry (attempt {RetryCount}/{MaxRetries})",
                commandId,
                entry.RetryCount + 1,
                entry.MaxRetries);

            return true;
        }
    }
}

