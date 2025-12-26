//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using Mvp24Hours.Infrastructure.Email.RateLimiting;
using Mvp24Hours.Infrastructure.Email.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Email.Bulk
{
    /// <summary>
    /// Service for sending emails in bulk with rate limiting and progress tracking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service provides efficient bulk email sending with built-in rate limiting to
    /// prevent exceeding email provider limits. It supports progress callbacks, error handling,
    /// and retry logic.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item><description>Rate limiting to prevent provider throttling</description></item>
    /// <item><description>Progress callbacks for monitoring</description></item>
    /// <item><description>Batch processing for efficiency</description></item>
    /// <item><description>Error handling and retry logic</description></item>
    /// <item><description>Parallel sending (configurable)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class EmailBulkSender
    {
        private readonly IEmailService _emailService;
        private readonly EmailRateLimiter? _rateLimiter;
        private readonly ILogger<EmailBulkSender>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailBulkSender"/> class.
        /// </summary>
        /// <param name="emailService">The email service to use for sending.</param>
        /// <param name="rateLimiter">Optional rate limiter to prevent exceeding provider limits.</param>
        /// <param name="logger">Optional logger.</param>
        public EmailBulkSender(
            IEmailService emailService,
            EmailRateLimiter? rateLimiter = null,
            ILogger<EmailBulkSender>? logger = null)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _rateLimiter = rateLimiter;
            _logger = logger;
        }

        /// <summary>
        /// Sends emails in bulk with rate limiting.
        /// </summary>
        /// <param name="messages">The email messages to send.</param>
        /// <param name="options">Bulk sending options.</param>
        /// <param name="progressCallback">Optional progress callback.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Bulk send result with statistics.</returns>
        public async Task<EmailBulkSendResult> SendBulkAsync(
            IEnumerable<EmailMessage> messages,
            BulkSendOptions? options = null,
            Action<BulkSendProgress>? progressCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            options ??= new BulkSendOptions();
            var messagesList = messages.ToList();
            var results = new List<EmailSendResult>();
            var startTime = DateTimeOffset.UtcNow;

            _logger?.LogInformation("Starting bulk email send: {Count} messages", messagesList.Count);

            if (options.MaxConcurrency > 1)
            {
                // Parallel sending
                await SendBulkParallelAsync(
                    messagesList,
                    options,
                    progressCallback,
                    results,
                    cancellationToken);
            }
            else
            {
                // Sequential sending
                await SendBulkSequentialAsync(
                    messagesList,
                    options,
                    progressCallback,
                    results,
                    cancellationToken);
            }

            var endTime = DateTimeOffset.UtcNow;
            var duration = endTime - startTime;

            var bulkResult = new EmailBulkSendResult
            {
                TotalCount = messagesList.Count,
                SuccessCount = results.Count(r => r.Success),
                FailureCount = results.Count(r => !r.Success),
                Results = results,
                Duration = duration,
                StartTime = startTime,
                EndTime = endTime
            };

            _logger?.LogInformation(
                "Bulk email send completed: {SuccessCount}/{TotalCount} succeeded in {Duration}ms",
                bulkResult.SuccessCount,
                bulkResult.TotalCount,
                duration.TotalMilliseconds);

            return bulkResult;
        }

        private async Task SendBulkSequentialAsync(
            List<EmailMessage> messages,
            BulkSendOptions options,
            Action<BulkSendProgress>? progressCallback,
            List<EmailSendResult> results,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < messages.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var message = messages[i];
                EmailSendResult result;

                try
                {
                    // Wait for rate limit if configured
                    if (_rateLimiter != null)
                    {
                        await _rateLimiter.WaitAsync(cancellationToken);
                    }

                    result = await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error sending email {Index}/{Total}", i + 1, messages.Count);
                    result = EmailSendResult.Failed(ex);
                }

                results.Add(result);

                // Report progress
                progressCallback?.Invoke(new BulkSendProgress
                {
                    ProcessedCount = i + 1,
                    TotalCount = messages.Count,
                    SuccessCount = results.Count(r => r.Success),
                    FailureCount = results.Count(r => !r.Success)
                });

                // Delay between sends if configured
                if (options.DelayBetweenSends > TimeSpan.Zero && i < messages.Count - 1)
                {
                    await Task.Delay(options.DelayBetweenSends, cancellationToken);
                }
            }
        }

        private async Task SendBulkParallelAsync(
            List<EmailMessage> messages,
            BulkSendOptions options,
            Action<BulkSendProgress>? progressCallback,
            List<EmailSendResult> results,
            CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(options.MaxConcurrency, options.MaxConcurrency);
            var tasks = new List<Task>();

            for (int i = 0; i < messages.Count; i++)
            {
                var index = i;
                var message = messages[index];

                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        EmailSendResult result;

                        try
                        {
                            // Wait for rate limit if configured
                            if (_rateLimiter != null)
                            {
                                await _rateLimiter.WaitAsync(cancellationToken);
                            }

                            result = await _emailService.SendAsync(message, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "Error sending email {Index}/{Total}", index + 1, messages.Count);
                            result = EmailSendResult.Failed(ex);
                        }

                        lock (results)
                        {
                            results.Add(result);

                            // Report progress
                            progressCallback?.Invoke(new BulkSendProgress
                            {
                                ProcessedCount = results.Count,
                                TotalCount = messages.Count,
                                SuccessCount = results.Count(r => r.Success),
                                FailureCount = results.Count(r => !r.Success)
                            });
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);

                // Delay between starting tasks if configured
                if (options.DelayBetweenSends > TimeSpan.Zero && index < messages.Count - 1)
                {
                    await Task.Delay(options.DelayBetweenSends, cancellationToken);
                }
            }

            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Options for bulk email sending.
    /// </summary>
    public class BulkSendOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of concurrent sends.
        /// </summary>
        /// <remarks>
        /// Set to 1 for sequential sending, or higher for parallel sending.
        /// Default is 1 (sequential).
        /// </remarks>
        public int MaxConcurrency { get; set; } = 1;

        /// <summary>
        /// Gets or sets the delay between sends.
        /// </summary>
        /// <remarks>
        /// This delay is applied between each email send to prevent overwhelming
        /// the email provider. Set to TimeSpan.Zero to disable.
        /// </remarks>
        public TimeSpan DelayBetweenSends { get; set; } = TimeSpan.Zero;
    }

    /// <summary>
    /// Progress information for bulk email sending.
    /// </summary>
    public class BulkSendProgress
    {
        /// <summary>
        /// Gets or sets the number of emails processed so far.
        /// </summary>
        public int ProcessedCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of emails to send.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the number of successful sends.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the number of failed sends.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Gets the progress percentage (0-100).
        /// </summary>
        public double ProgressPercentage => TotalCount > 0 ? (ProcessedCount * 100.0 / TotalCount) : 0;
    }

    /// <summary>
    /// Result of bulk email sending operation.
    /// </summary>
    public class EmailBulkSendResult
    {
        /// <summary>
        /// Gets or sets the total number of emails sent.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the number of successful sends.
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the number of failed sends.
        /// </summary>
        public int FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the individual send results.
        /// </summary>
        public IList<EmailSendResult> Results { get; set; } = new List<EmailSendResult>();

        /// <summary>
        /// Gets or sets the duration of the bulk send operation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets when the bulk send started.
        /// </summary>
        public DateTimeOffset StartTime { get; set; }

        /// <summary>
        /// Gets or sets when the bulk send completed.
        /// </summary>
        public DateTimeOffset EndTime { get; set; }

        /// <summary>
        /// Gets the success rate (0-1).
        /// </summary>
        public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount : 0;
    }
}

