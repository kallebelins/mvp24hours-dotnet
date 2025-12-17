//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Scheduling
{
    /// <summary>
    /// Implementation of the message scheduler using a polling-based approach.
    /// Messages are stored and periodically checked for delivery.
    /// </summary>
    public class MessageScheduler : IMessageScheduler
    {
        private readonly IScheduledMessageStore _store;
        private readonly IMvpRabbitMQClient _client;
        private readonly MessageSchedulerOptions _options;
        private readonly ILogger<MessageScheduler>? _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the MessageScheduler.
        /// </summary>
        /// <param name="store">The scheduled message store.</param>
        /// <param name="client">The RabbitMQ client for publishing messages.</param>
        /// <param name="options">The scheduler options.</param>
        /// <param name="logger">Optional logger.</param>
        public MessageScheduler(
            IScheduledMessageStore store,
            IMvpRabbitMQClient client,
            IOptions<MessageSchedulerOptions> options,
            ILogger<MessageScheduler>? logger = null)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _options = options?.Value ?? new MessageSchedulerOptions();
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        /// <inheritdoc />
        public Task<Guid> ScheduleMessageAsync<T>(
            DateTime scheduledTime,
            T message,
            string? routingKey = null,
            CancellationToken cancellationToken = default) where T : class
        {
            return ScheduleMessageAsync(
                new DateTimeOffset(scheduledTime.ToUniversalTime()),
                message,
                new ScheduleMessageOptions { RoutingKey = routingKey },
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Guid> ScheduleMessageAsync<T>(
            DateTimeOffset scheduledTime,
            T message,
            ScheduleMessageOptions? options = null,
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(message);

            if (scheduledTime <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentException("Scheduled time must be in the future.", nameof(scheduledTime));
            }

            var scheduledMessage = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(message, _jsonOptions),
                MessageType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
                RoutingKey = options?.RoutingKey ?? string.Empty,
                Exchange = options?.Exchange,
                ScheduledTime = scheduledTime,
                CorrelationId = options?.CorrelationId ?? Guid.NewGuid().ToString(),
                Headers = options?.Headers,
                Priority = options?.Priority,
                TtlMilliseconds = options?.TtlMilliseconds,
                Status = ScheduledMessageStatus.Pending
            };

            await _store.AddAsync(scheduledMessage, cancellationToken);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "rabbitmq-scheduler-message-scheduled",
                $"id:{scheduledMessage.Id}|scheduledTime:{scheduledTime:O}|type:{typeof(T).Name}");

            _logger?.LogInformation(
                "Scheduled message {MessageId} of type {MessageType} for {ScheduledTime}",
                scheduledMessage.Id, typeof(T).Name, scheduledTime);

            return scheduledMessage.Id;
        }

        /// <inheritdoc />
        public Task<Guid> ScheduleMessageAsync<T>(
            TimeSpan delay,
            T message,
            string? routingKey = null,
            CancellationToken cancellationToken = default) where T : class
        {
            if (delay <= TimeSpan.Zero)
            {
                throw new ArgumentException("Delay must be positive.", nameof(delay));
            }

            var scheduledTime = DateTimeOffset.UtcNow.Add(delay);
            return ScheduleMessageAsync(
                scheduledTime,
                message,
                new ScheduleMessageOptions { RoutingKey = routingKey },
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Guid> ScheduleRecurringMessageAsync<T>(
            TimeSpan interval,
            T message,
            string? routingKey = null,
            DateTimeOffset? startTime = null,
            int? maxExecutions = null,
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(message);

            if (interval < _options.MinimumRecurringInterval)
            {
                throw new ArgumentException(
                    $"Interval must be at least {_options.MinimumRecurringInterval.TotalMinutes} minutes.",
                    nameof(interval));
            }

            var firstExecution = startTime ?? DateTimeOffset.UtcNow.Add(interval);

            var scheduledMessage = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(message, _jsonOptions),
                MessageType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
                RoutingKey = routingKey ?? string.Empty,
                ScheduledTime = firstExecution,
                NextExecutionTime = firstExecution,
                CorrelationId = Guid.NewGuid().ToString(),
                Status = ScheduledMessageStatus.Active,
                RecurringSchedule = new RecurringSchedule
                {
                    Type = RecurringScheduleType.Interval,
                    Interval = interval,
                    MaxExecutions = maxExecutions
                }
            };

            await _store.AddAsync(scheduledMessage, cancellationToken);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "rabbitmq-scheduler-recurring-message-scheduled",
                $"id:{scheduledMessage.Id}|interval:{interval}|type:{typeof(T).Name}");

            _logger?.LogInformation(
                "Scheduled recurring message {MessageId} of type {MessageType} with interval {Interval}",
                scheduledMessage.Id, typeof(T).Name, interval);

            return scheduledMessage.Id;
        }

        /// <inheritdoc />
        public async Task<Guid> ScheduleRecurringMessageAsync<T>(
            string cronExpression,
            T message,
            string? routingKey = null,
            string timeZone = "UTC",
            int? maxExecutions = null,
            CancellationToken cancellationToken = default) where T : class
        {
            ArgumentNullException.ThrowIfNull(message);

            if (!CronExpressionHelper.IsValid(cronExpression))
            {
                throw new ArgumentException("Invalid CRON expression.", nameof(cronExpression));
            }

            var nextExecution = CronExpressionHelper.GetNextOccurrence(cronExpression, null, timeZone);
            if (!nextExecution.HasValue)
            {
                throw new ArgumentException("Could not calculate next execution time from CRON expression.", nameof(cronExpression));
            }

            var scheduledMessage = new ScheduledMessage
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(message, _jsonOptions),
                MessageType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
                RoutingKey = routingKey ?? string.Empty,
                ScheduledTime = nextExecution.Value,
                NextExecutionTime = nextExecution.Value,
                CorrelationId = Guid.NewGuid().ToString(),
                Status = ScheduledMessageStatus.Active,
                RecurringSchedule = new RecurringSchedule
                {
                    Type = RecurringScheduleType.Cron,
                    CronExpression = cronExpression,
                    TimeZone = timeZone,
                    MaxExecutions = maxExecutions
                }
            };

            await _store.AddAsync(scheduledMessage, cancellationToken);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "rabbitmq-scheduler-cron-message-scheduled",
                $"id:{scheduledMessage.Id}|cron:{cronExpression}|type:{typeof(T).Name}");

            _logger?.LogInformation(
                "Scheduled CRON message {MessageId} of type {MessageType} with expression {CronExpression}",
                scheduledMessage.Id, typeof(T).Name, cronExpression);

            return scheduledMessage.Id;
        }

        /// <inheritdoc />
        public async Task<bool> CancelScheduledMessageAsync(
            Guid scheduledMessageId,
            CancellationToken cancellationToken = default)
        {
            var message = await _store.GetByIdAsync(scheduledMessageId, cancellationToken);
            if (message == null)
            {
                return false;
            }

            if (message.Status == ScheduledMessageStatus.Completed ||
                message.Status == ScheduledMessageStatus.Cancelled ||
                message.Status == ScheduledMessageStatus.Processing)
            {
                return false;
            }

            message.Status = ScheduledMessageStatus.Cancelled;
            message.ProcessedAt = DateTimeOffset.UtcNow;
            await _store.UpdateAsync(message, cancellationToken);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "rabbitmq-scheduler-message-cancelled",
                $"id:{scheduledMessageId}");

            _logger?.LogInformation("Cancelled scheduled message {MessageId}", scheduledMessageId);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> PauseRecurringMessageAsync(
            Guid recurringMessageId,
            CancellationToken cancellationToken = default)
        {
            var message = await _store.GetByIdAsync(recurringMessageId, cancellationToken);
            if (message == null || !message.IsRecurring)
            {
                return false;
            }

            if (message.Status != ScheduledMessageStatus.Active)
            {
                return false;
            }

            message.Status = ScheduledMessageStatus.Paused;
            await _store.UpdateAsync(message, cancellationToken);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "rabbitmq-scheduler-recurring-message-paused",
                $"id:{recurringMessageId}");

            _logger?.LogInformation("Paused recurring message {MessageId}", recurringMessageId);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> ResumeRecurringMessageAsync(
            Guid recurringMessageId,
            CancellationToken cancellationToken = default)
        {
            var message = await _store.GetByIdAsync(recurringMessageId, cancellationToken);
            if (message == null || !message.IsRecurring)
            {
                return false;
            }

            if (message.Status != ScheduledMessageStatus.Paused)
            {
                return false;
            }

            // Recalculate next execution time
            if (message.RecurringSchedule!.Type == RecurringScheduleType.Cron)
            {
                var nextExecution = CronExpressionHelper.GetNextOccurrence(
                    message.RecurringSchedule.CronExpression!,
                    null,
                    message.RecurringSchedule.TimeZone);
                message.NextExecutionTime = nextExecution;
            }
            else
            {
                message.NextExecutionTime = DateTimeOffset.UtcNow.Add(message.RecurringSchedule.Interval!.Value);
            }

            message.Status = ScheduledMessageStatus.Active;
            await _store.UpdateAsync(message, cancellationToken);

            TelemetryHelper.Execute(
                TelemetryLevels.Information,
                "rabbitmq-scheduler-recurring-message-resumed",
                $"id:{recurringMessageId}|nextExecution:{message.NextExecutionTime:O}");

            _logger?.LogInformation(
                "Resumed recurring message {MessageId}, next execution at {NextExecutionTime}",
                recurringMessageId, message.NextExecutionTime);

            return true;
        }

        /// <inheritdoc />
        public Task<ScheduledMessage?> GetScheduledMessageAsync(
            Guid scheduledMessageId,
            CancellationToken cancellationToken = default)
        {
            return _store.GetByIdAsync(scheduledMessageId, cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ScheduledMessage>> GetPendingMessagesAsync(
            CancellationToken cancellationToken = default)
        {
            return _store.GetPendingMessagesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ScheduledMessage>> GetActiveRecurringMessagesAsync(
            CancellationToken cancellationToken = default)
        {
            return _store.GetActiveRecurringMessagesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<IEnumerable<ScheduledMessage>> GetDueMessagesAsync(
            int batchSize = 100,
            CancellationToken cancellationToken = default)
        {
            return _store.GetDueMessagesAsync(batchSize, cancellationToken);
        }

        /// <summary>
        /// Processes due messages and publishes them to RabbitMQ.
        /// This method is called by the background service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of messages processed.</returns>
        public async Task<int> ProcessDueMessagesAsync(CancellationToken cancellationToken = default)
        {
            var dueMessages = await _store.GetDueMessagesAsync(_options.BatchSize, cancellationToken);
            var processed = 0;

            foreach (var message in dueMessages)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // Mark as processing to prevent duplicate processing
                    if (!await _store.MarkAsProcessingAsync(message.Id, cancellationToken))
                    {
                        continue; // Already being processed by another instance
                    }

                    await PublishMessageAsync(message, cancellationToken);
                    processed++;

                    if (message.IsRecurring)
                    {
                        await HandleRecurringMessageCompletionAsync(message, cancellationToken);
                    }
                    else
                    {
                        await _store.MarkAsCompletedAsync(message.Id, cancellationToken);
                    }

                    TelemetryHelper.Execute(
                        TelemetryLevels.Information,
                        "rabbitmq-scheduler-message-delivered",
                        $"id:{message.Id}|type:{message.MessageType}");

                    _logger?.LogDebug("Delivered scheduled message {MessageId}", message.Id);
                }
                catch (Exception ex)
                {
                    await HandleMessageFailureAsync(message, ex, cancellationToken);
                }
            }

            return processed;
        }

        private async Task PublishMessageAsync(ScheduledMessage message, CancellationToken cancellationToken)
        {
            // Prepare headers
            var headers = message.Headers ?? new Dictionary<string, object>();
            headers["x-scheduled-message-id"] = message.Id.ToString();
            headers["x-scheduled-time"] = message.ScheduledTime.ToString("O");
            headers["x-message-type"] = message.MessageType;

            if (message.IsRecurring)
            {
                headers["x-execution-count"] = message.ExecutionCount + 1;
            }

            // Deserialize the message to its original type
            var messageType = Type.GetType(message.MessageType);
            object? payload;

            if (messageType != null)
            {
                payload = JsonSerializer.Deserialize(message.Payload, messageType, _jsonOptions);
            }
            else
            {
                // Fallback to dynamic object
                payload = JsonSerializer.Deserialize<object>(message.Payload, _jsonOptions);
            }

            if (payload == null)
            {
                throw new InvalidOperationException($"Failed to deserialize message payload for message {message.Id}");
            }

            // Publish with appropriate options
            if (message.Priority.HasValue)
            {
                _client.Publish(payload, message.RoutingKey, message.Priority.Value, message.CorrelationId);
            }
            else if (message.Headers != null && message.Headers.Count > 0)
            {
                _client.Publish(payload, message.RoutingKey, headers, message.CorrelationId);
            }
            else if (message.TtlMilliseconds.HasValue)
            {
                _client.PublishWithTtl(payload, message.RoutingKey, message.TtlMilliseconds.Value, message.CorrelationId);
            }
            else
            {
                _client.Publish(payload, message.RoutingKey, message.CorrelationId);
            }
        }

        private async Task HandleRecurringMessageCompletionAsync(
            ScheduledMessage message,
            CancellationToken cancellationToken)
        {
            message.ExecutionCount++;
            message.ProcessedAt = DateTimeOffset.UtcNow;

            // Check if max executions reached
            if (message.RecurringSchedule!.MaxExecutions.HasValue &&
                message.ExecutionCount >= message.RecurringSchedule.MaxExecutions.Value)
            {
                message.Status = ScheduledMessageStatus.Completed;
                await _store.UpdateAsync(message, cancellationToken);

                _logger?.LogInformation(
                    "Recurring message {MessageId} completed after {ExecutionCount} executions",
                    message.Id, message.ExecutionCount);
                return;
            }

            // Check if end date reached
            if (message.RecurringSchedule.EndDate.HasValue &&
                DateTimeOffset.UtcNow >= message.RecurringSchedule.EndDate.Value)
            {
                message.Status = ScheduledMessageStatus.Completed;
                await _store.UpdateAsync(message, cancellationToken);

                _logger?.LogInformation(
                    "Recurring message {MessageId} completed - end date reached",
                    message.Id);
                return;
            }

            // Calculate next execution time
            if (message.RecurringSchedule.Type == RecurringScheduleType.Cron)
            {
                var nextExecution = CronExpressionHelper.GetNextOccurrence(
                    message.RecurringSchedule.CronExpression!,
                    DateTimeOffset.UtcNow,
                    message.RecurringSchedule.TimeZone);
                message.NextExecutionTime = nextExecution;
            }
            else
            {
                message.NextExecutionTime = DateTimeOffset.UtcNow.Add(message.RecurringSchedule.Interval!.Value);
            }

            message.Status = ScheduledMessageStatus.Active;
            message.RetryCount = 0;
            message.LastError = null;

            await _store.UpdateAsync(message, cancellationToken);

            _logger?.LogDebug(
                "Recurring message {MessageId} scheduled for next execution at {NextExecutionTime}",
                message.Id, message.NextExecutionTime);
        }

        private async Task HandleMessageFailureAsync(
            ScheduledMessage message,
            Exception ex,
            CancellationToken cancellationToken)
        {
            message.RetryCount++;
            message.LastError = ex.Message;

            TelemetryHelper.Execute(
                TelemetryLevels.Warning,
                "rabbitmq-scheduler-message-failed",
                $"id:{message.Id}|retry:{message.RetryCount}|error:{ex.Message}");

            _logger?.LogWarning(ex,
                "Failed to deliver scheduled message {MessageId}, attempt {RetryCount}/{MaxRetries}",
                message.Id, message.RetryCount, _options.MaxRetryCount);

            if (message.RetryCount >= _options.MaxRetryCount)
            {
                await _store.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);

                _logger?.LogError(
                    "Scheduled message {MessageId} failed after {MaxRetries} retries",
                    message.Id, _options.MaxRetryCount);
            }
            else
            {
                // Schedule retry with exponential backoff
                var delay = TimeSpan.FromMilliseconds(
                    _options.BaseRetryDelayMs * Math.Pow(_options.RetryDelayMultiplier, message.RetryCount - 1));

                if (message.IsRecurring)
                {
                    message.NextExecutionTime = DateTimeOffset.UtcNow.Add(delay);
                    message.Status = ScheduledMessageStatus.Active;
                }
                else
                {
                    message.ScheduledTime = DateTimeOffset.UtcNow.Add(delay);
                    message.Status = ScheduledMessageStatus.Pending;
                }

                await _store.UpdateAsync(message, cancellationToken);
            }
        }
    }
}

