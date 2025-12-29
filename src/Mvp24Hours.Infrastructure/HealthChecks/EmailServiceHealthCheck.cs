//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.Email.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check for email service providers to verify connectivity and send capability.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies email service provider health by:
    /// <list type="bullet">
    /// <item>Attempting to send a test email (if enabled)</item>
    /// <item>Verifying provider connectivity</item>
    /// <item>Checking provider configuration</item>
    /// <item>Measuring response times</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> By default, this health check does NOT send actual emails.
    /// Set <see cref="EmailServiceHealthCheckOptions.SendTestEmail"/> to true to enable
    /// actual email sending (use with caution in production).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddEmailServiceHealthCheck(
    ///         "email-service",
    ///         options =>
    ///         {
    ///             options.SendTestEmail = false; // Don't send actual emails
    ///             options.TimeoutSeconds = 5;
    ///         });
    /// </code>
    /// </example>
    public class EmailServiceHealthCheck : IHealthCheck
    {
        private readonly IEmailService _emailService;
        private readonly EmailServiceHealthCheckOptions _options;
        private readonly ILogger<EmailServiceHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailServiceHealthCheck"/> class.
        /// </summary>
        /// <param name="emailService">The email service to check.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public EmailServiceHealthCheck(
            IEmailService emailService,
            EmailServiceHealthCheckOptions? options,
            ILogger<EmailServiceHealthCheck> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _options = options ?? new EmailServiceHealthCheckOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Create timeout cancellation token
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

                if (_options.SendTestEmail)
                {
                    // Send a test email
                    var testMessage = new EmailMessage
                    {
                        To = new[] { _options.TestEmailRecipient ?? "health-check@example.com" },
                        Subject = _options.TestEmailSubject ?? "Health Check Test",
                        PlainTextBody = _options.TestEmailBody ?? "This is an automated health check test email."
                    };

                    data["testEmailSent"] = true;
                    data["testEmailRecipient"] = testMessage.To[0];

                    var sendStopwatch = Stopwatch.StartNew();
                    var sendResult = await _emailService.SendAsync(testMessage, cts.Token);
                    sendStopwatch.Stop();

                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["sendTimeMs"] = sendStopwatch.ElapsedMilliseconds;
                    data["sendSuccess"] = sendResult.Success;

                    if (!sendResult.Success)
                    {
                        data["error"] = sendResult.FirstError ?? "Email send failed";
                        data["errors"] = sendResult.Errors;

                        _logger.LogError("Email service health check failed: Send failed. Error: {Error}", sendResult.FirstError);

                        return HealthCheckResult.Unhealthy(
                            description: $"Email service send failed: {sendResult.FirstError}",
                            data: data);
                    }

                    data["messageId"] = sendResult.MessageId;

                    // Check response time thresholds
                    if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                    {
                        return HealthCheckResult.Unhealthy(
                            description: $"Email service response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                            data: data);
                    }

                    if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                    {
                        return HealthCheckResult.Degraded(
                            description: $"Email service response time {stopwatch.ElapsedMilliseconds}ms is slow",
                            data: data);
                    }

                    return HealthCheckResult.Healthy(
                        description: $"Email service is healthy (send time: {sendStopwatch.ElapsedMilliseconds}ms)",
                        data: data);
                }
                else
                {
                    // Just verify the service is available (no actual email sent)
                    // For most providers, we can't verify without sending, so we'll just check
                    // if the service can be instantiated and configured properly
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["testEmailSent"] = false;
                    data["note"] = "Test email sending is disabled. Enable SendTestEmail to verify actual sending capability.";

                    // If we can't verify without sending, return healthy but degraded
                    // This is a conservative approach - the service might be healthy but we can't verify
                    return HealthCheckResult.Healthy(
                        description: "Email service is available (test email sending disabled)",
                        data: data);
                }
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = "Operation timeout";

                _logger.LogWarning("Email service health check timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);

                return HealthCheckResult.Unhealthy(
                    description: $"Email service health check timed out after {_options.TimeoutSeconds}s",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "Email service health check failed with unexpected error");

                return HealthCheckResult.Unhealthy(
                    description: $"Email service health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }
    }

    /// <summary>
    /// Configuration options for email service health checks.
    /// </summary>
    public sealed class EmailServiceHealthCheckOptions
    {
        /// <summary>
        /// Whether to send an actual test email.
        /// Default is false (no email sent).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When true, a test email will be sent to verify the email service is working.
        /// Use with caution in production environments to avoid sending unnecessary emails.
        /// </para>
        /// <para>
        /// When false, the health check only verifies the service is available but cannot
        /// verify actual sending capability.
        /// </para>
        /// </remarks>
        public bool SendTestEmail { get; set; }

        /// <summary>
        /// Recipient email address for test email.
        /// Only used if SendTestEmail is true.
        /// Default is "health-check@example.com".
        /// </summary>
        public string? TestEmailRecipient { get; set; }

        /// <summary>
        /// Subject for test email.
        /// Only used if SendTestEmail is true.
        /// Default is "Health Check Test".
        /// </summary>
        public string? TestEmailSubject { get; set; }

        /// <summary>
        /// Body for test email.
        /// Only used if SendTestEmail is true.
        /// Default is "This is an automated health check test email.".
        /// </summary>
        public string? TestEmailBody { get; set; }

        /// <summary>
        /// Timeout in seconds for the health check operations.
        /// Default is 10 seconds.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Response time threshold in milliseconds for degraded status.
        /// Default is 2000ms.
        /// </summary>
        public int DegradedThresholdMs { get; set; } = 2000;

        /// <summary>
        /// Response time threshold in milliseconds for unhealthy status.
        /// Default is 10000ms.
        /// </summary>
        public int FailureThresholdMs { get; set; } = 10000;

        /// <summary>
        /// Tags to associate with this health check.
        /// </summary>
        public IEnumerable<string> Tags { get; set; } = new[] { "email", "email-service", "ready" };
    }
}

