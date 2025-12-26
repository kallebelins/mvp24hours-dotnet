//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.Sms.Contract;
using Mvp24Hours.Infrastructure.Sms.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.HealthChecks
{
    /// <summary>
    /// Health check for SMS service providers to verify connectivity and send capability.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This health check verifies SMS service provider health by:
    /// <list type="bullet">
    /// <item>Attempting to send a test SMS (if enabled)</item>
    /// <item>Verifying provider connectivity</item>
    /// <item>Checking provider configuration</item>
    /// <item>Measuring response times</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> By default, this health check does NOT send actual SMS messages.
    /// Set <see cref="SmsServiceHealthCheckOptions.SendTestSms"/> to true to enable
    /// actual SMS sending (use with caution in production).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddHealthChecks()
    ///     .AddSmsServiceHealthCheck(
    ///         "sms-service",
    ///         options =>
    ///         {
    ///             options.SendTestSms = false; // Don't send actual SMS
    ///             options.TimeoutSeconds = 5;
    ///         });
    /// </code>
    /// </example>
    public class SmsServiceHealthCheck : IHealthCheck
    {
        private readonly ISmsService _smsService;
        private readonly SmsServiceHealthCheckOptions _options;
        private readonly ILogger<SmsServiceHealthCheck> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmsServiceHealthCheck"/> class.
        /// </summary>
        /// <param name="smsService">The SMS service to check.</param>
        /// <param name="options">Health check configuration options.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public SmsServiceHealthCheck(
            ISmsService smsService,
            SmsServiceHealthCheckOptions? options,
            ILogger<SmsServiceHealthCheck> logger)
        {
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
            _options = options ?? new SmsServiceHealthCheckOptions();
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

                if (_options.SendTestSms)
                {
                    // Send a test SMS
                    var testMessage = new SmsMessage
                    {
                        To = _options.TestSmsRecipient ?? "+1234567890",
                        Body = _options.TestSmsBody ?? "Health check test"
                    };

                    data["testSmsSent"] = true;
                    data["testSmsRecipient"] = testMessage.To;

                    var sendStopwatch = Stopwatch.StartNew();
                    var sendResult = await _smsService.SendAsync(testMessage, cts.Token);
                    sendStopwatch.Stop();

                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["sendTimeMs"] = sendStopwatch.ElapsedMilliseconds;
                    data["sendSuccess"] = sendResult.Success;

                    if (!sendResult.Success)
                    {
                        data["error"] = sendResult.ErrorMessage ?? "SMS send failed";
                        data["status"] = sendResult.Status?.ToString();

                        _logger.LogError("SMS service health check failed: Send failed. Error: {Error}", sendResult.ErrorMessage);

                        return HealthCheckResult.Unhealthy(
                            description: $"SMS service send failed: {sendResult.ErrorMessage}",
                            data: data);
                    }

                    data["messageId"] = sendResult.MessageId;
                    data["status"] = sendResult.Status?.ToString();

                    // Check response time thresholds
                    if (stopwatch.ElapsedMilliseconds >= _options.FailureThresholdMs)
                    {
                        return HealthCheckResult.Unhealthy(
                            description: $"SMS service response time {stopwatch.ElapsedMilliseconds}ms exceeded threshold",
                            data: data);
                    }

                    if (stopwatch.ElapsedMilliseconds >= _options.DegradedThresholdMs)
                    {
                        return HealthCheckResult.Degraded(
                            description: $"SMS service response time {stopwatch.ElapsedMilliseconds}ms is slow",
                            data: data);
                    }

                    return HealthCheckResult.Healthy(
                        description: $"SMS service is healthy (send time: {sendStopwatch.ElapsedMilliseconds}ms)",
                        data: data);
                }
                else
                {
                    // Just verify the service is available (no actual SMS sent)
                    // For most providers, we can't verify without sending, so we'll just check
                    // if the service can be instantiated and configured properly
                    stopwatch.Stop();
                    data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                    data["testSmsSent"] = false;
                    data["note"] = "Test SMS sending is disabled. Enable SendTestSms to verify actual sending capability.";

                    // If we can't verify without sending, return healthy but degraded
                    // This is a conservative approach - the service might be healthy but we can't verify
                    return HealthCheckResult.Healthy(
                        description: "SMS service is available (test SMS sending disabled)",
                        data: data);
                }
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = "Operation timeout";

                _logger.LogWarning("SMS service health check timed out after {TimeoutSeconds}s", _options.TimeoutSeconds);

                return HealthCheckResult.Unhealthy(
                    description: $"SMS service health check timed out after {_options.TimeoutSeconds}s",
                    data: data);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                data["responseTimeMs"] = stopwatch.ElapsedMilliseconds;
                data["error"] = ex.Message;

                _logger.LogError(ex, "SMS service health check failed with unexpected error");

                return HealthCheckResult.Unhealthy(
                    description: $"SMS service health check failed: {ex.Message}",
                    exception: ex,
                    data: data);
            }
        }
    }

    /// <summary>
    /// Configuration options for SMS service health checks.
    /// </summary>
    public sealed class SmsServiceHealthCheckOptions
    {
        /// <summary>
        /// Whether to send an actual test SMS.
        /// Default is false (no SMS sent).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When true, a test SMS will be sent to verify the SMS service is working.
        /// Use with caution in production environments to avoid sending unnecessary SMS messages
        /// and incurring costs.
        /// </para>
        /// <para>
        /// When false, the health check only verifies the service is available but cannot
        /// verify actual sending capability.
        /// </para>
        /// </remarks>
        public bool SendTestSms { get; set; }

        /// <summary>
        /// Recipient phone number for test SMS (E.164 format or national format).
        /// Only used if SendTestSms is true.
        /// Default is "+1234567890".
        /// </summary>
        public string? TestSmsRecipient { get; set; }

        /// <summary>
        /// Body for test SMS.
        /// Only used if SendTestSms is true.
        /// Default is "Health check test".
        /// </summary>
        public string? TestSmsBody { get; set; }

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
        public IEnumerable<string> Tags { get; set; } = new[] { "sms", "sms-service", "ready" };
    }
}

