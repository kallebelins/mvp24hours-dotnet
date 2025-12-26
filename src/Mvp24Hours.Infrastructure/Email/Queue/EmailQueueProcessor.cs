//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Queue
{
    /// <summary>
    /// Background service that processes queued email messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This background service continuously processes emails from the queue, sending them
    /// asynchronously. It handles retries, error logging, and status updates.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item><description>Automatic processing of queued emails</description></item>
    /// <item><description>Retry logic for failed sends</description></item>
    /// <item><description>Status tracking and updates</description></item>
    /// <item><description>Graceful shutdown</description></item>
    /// <item><description>Configurable polling interval</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class EmailQueueProcessor : BackgroundService
    {
        private readonly IEmailQueue _emailQueue;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailQueueProcessor>? _logger;
        private readonly EmailQueueProcessorOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailQueueProcessor"/> class.
        /// </summary>
        /// <param name="emailQueue">The email queue to process.</param>
        /// <param name="emailService">The email service to use for sending.</param>
        /// <param name="options">Processor options.</param>
        /// <param name="logger">Optional logger.</param>
        public EmailQueueProcessor(
            IEmailQueue emailQueue,
            IEmailService emailService,
            IOptions<EmailQueueProcessorOptions> options,
            ILogger<EmailQueueProcessor>? logger = null)
        {
            _emailQueue = emailQueue ?? throw new ArgumentNullException(nameof(emailQueue));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("Email queue processor started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessQueueAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error processing email queue");
                }

                // Wait before next poll
                await Task.Delay(_options.PollInterval, stoppingToken);
            }

            _logger?.LogInformation("Email queue processor stopped");
        }

        /// <summary>
        /// Processes queued email messages.
        /// </summary>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            // Get next item from queue (if InMemoryEmailQueue)
            if (_emailQueue is InMemoryEmailQueue inMemoryQueue)
            {
                var item = inMemoryQueue.GetNextItem();
                if (item == null)
                {
                    return; // No items to process
                }

                await ProcessQueueItemAsync(inMemoryQueue, item, cancellationToken);
            }
            else
            {
                // For other queue implementations, they should provide their own processing mechanism
                _logger?.LogWarning("Email queue processor only supports InMemoryEmailQueue. Custom queue implementations should handle processing themselves.");
            }
        }

        /// <summary>
        /// Processes a single queue item.
        /// </summary>
        private async Task ProcessQueueItemAsync(
            InMemoryEmailQueue queue,
            IEmailQueueItem item,
            CancellationToken cancellationToken)
        {
            var queueItemId = item.QueueItemId;
            var message = item.Message;

            if (message == null)
            {
                _logger?.LogWarning("Queue item {QueueItemId} has null message", queueItemId);
                return;
            }

            // Check if scheduled time has passed
            if (item.ScheduledSendTime.HasValue && item.ScheduledSendTime > DateTimeOffset.UtcNow)
            {
                return; // Not time to send yet
            }

            // Mark as sending
            queue.MarkAsSending(queueItemId);

            _logger?.LogInformation("Processing email queue item {QueueItemId}", queueItemId);

            try
            {
                // Send email
                var result = await _emailService.SendAsync(message, cancellationToken);

                if (result.Success)
                {
                    queue.MarkAsSent(queueItemId, result.MessageId);
                    _logger?.LogInformation(
                        "Email queue item {QueueItemId} sent successfully. MessageId: {MessageId}",
                        queueItemId,
                        result.MessageId);
                }
                else
                {
                    // Check retry count
                    if (item.AttemptCount < _options.MaxRetryAttempts)
                    {
                        // Retry later - mark as queued again
                        queue.MarkAsFailed(queueItemId, string.Join("; ", result.Errors));
                        _logger?.LogWarning(
                            "Email queue item {QueueItemId} failed (attempt {AttemptCount}/{MaxRetries}). Will retry. Error: {Error}",
                            queueItemId,
                            item.AttemptCount,
                            _options.MaxRetryAttempts,
                            string.Join("; ", result.Errors));
                    }
                    else
                    {
                        // Max retries reached - mark as failed permanently
                        queue.MarkAsFailed(queueItemId, $"Max retries reached. Errors: {string.Join("; ", result.Errors)}");
                        _logger?.LogError(
                            "Email queue item {QueueItemId} failed after {MaxRetries} attempts. Error: {Error}",
                            queueItemId,
                            _options.MaxRetryAttempts,
                            string.Join("; ", result.Errors));
                    }
                }
            }
            catch (Exception ex)
            {
                // Check retry count
                if (item.AttemptCount < _options.MaxRetryAttempts)
                {
                    queue.MarkAsFailed(queueItemId, ex.Message);
                    _logger?.LogWarning(
                        ex,
                        "Email queue item {QueueItemId} threw exception (attempt {AttemptCount}/{MaxRetries}). Will retry.",
                        queueItemId,
                        item.AttemptCount,
                        _options.MaxRetryAttempts);
                }
                else
                {
                    queue.MarkAsFailed(queueItemId, $"Max retries reached. Exception: {ex.Message}");
                    _logger?.LogError(
                        ex,
                        "Email queue item {QueueItemId} failed after {MaxRetries} attempts.",
                        queueItemId,
                        _options.MaxRetryAttempts);
                }
            }
        }
    }

    /// <summary>
    /// Options for email queue processor.
    /// </summary>
    public class EmailQueueProcessorOptions
    {
        /// <summary>
        /// Gets or sets the polling interval (how often to check for new emails).
        /// </summary>
        /// <remarks>
        /// Default is 5 seconds. Lower values provide faster processing but consume more resources.
        /// </remarks>
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed emails.
        /// </summary>
        /// <remarks>
        /// Default is 3. After this many attempts, the email will be marked as permanently failed.
        /// </remarks>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// </summary>
        /// <remarks>
        /// Default is 1 minute. This delay is applied before retrying a failed email.
        /// </remarks>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the maximum number of concurrent email sends.
        /// </summary>
        /// <remarks>
        /// Default is 1 (sequential). Increase for parallel processing, but be mindful of
        /// email provider rate limits.
        /// </remarks>
        public int MaxConcurrency { get; set; } = 1;
    }
}

