//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.WebAPI.HealthChecks
{
    /// <summary>
    /// Base class for custom health checks with common functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This base class provides:
    /// <list type="bullet">
    /// <item>Structured logging support</item>
    /// <item>Exception handling</item>
    /// <item>Timeout handling</item>
    /// <item>Consistent data dictionary structure</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyCustomHealthCheck : BaseHealthCheck
    /// {
    ///     public MyCustomHealthCheck(ILogger&lt;MyCustomHealthCheck&gt; logger)
    ///         : base(logger)
    ///     {
    ///     }
    /// 
    ///     protected override async Task&lt;HealthCheckResult&gt; CheckHealthAsyncCore(
    ///         HealthCheckContext context,
    ///         CancellationToken cancellationToken)
    ///     {
    ///         // Your health check logic here
    ///         var isHealthy = await CheckSomethingAsync();
    ///         
    ///         if (isHealthy)
    ///         {
    ///             return HealthCheckResult.Healthy("Service is healthy", GetData());
    ///         }
    ///         
    ///         return HealthCheckResult.Unhealthy("Service is unhealthy", data: GetData());
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class BaseHealthCheck : IHealthCheck
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseHealthCheck"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        protected BaseHealthCheck(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        protected ILogger Logger => _logger;

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var checkName = context.Registration.Name;
            var startTime = DateTimeOffset.UtcNow;

            try
            {
                _logger.LogDebug("Starting health check: {CheckName}", checkName);

                var result = await CheckHealthAsyncCore(context, cancellationToken);

                var duration = DateTimeOffset.UtcNow - startTime;
                _logger.LogDebug(
                    "Health check completed: {CheckName}, Status: {Status}, Duration: {Duration}ms",
                    checkName,
                    result.Status,
                    duration.TotalMilliseconds);

                // Add timing information to result data
                var enrichedData = new Dictionary<string, object>(result.Data ?? new Dictionary<string, object>())
                {
                    ["duration_ms"] = duration.TotalMilliseconds,
                    ["timestamp"] = startTime
                };

                if (result.Exception != null)
                {
                    return new HealthCheckResult(
                        result.Status,
                        result.Description,
                        result.Exception,
                        enrichedData);
                }
                return new HealthCheckResult(
                    result.Status,
                    result.Description,
                    data: enrichedData);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Health check cancelled: {CheckName}", checkName);
                throw;
            }
            catch (Exception ex)
            {
                var duration = DateTimeOffset.UtcNow - startTime;
                _logger.LogError(
                    ex,
                    "Health check failed with exception: {CheckName}, Duration: {Duration}ms",
                    checkName,
                    duration.TotalMilliseconds);

                return HealthCheckResult.Unhealthy(
                    $"Health check '{checkName}' failed with exception: {ex.Message}",
                    ex,
                    new Dictionary<string, object>
                    {
                        ["duration_ms"] = duration.TotalMilliseconds,
                        ["timestamp"] = startTime,
                        ["error"] = ex.Message,
                        ["exceptionType"] = ex.GetType().Name
                    });
            }
        }

        /// <summary>
        /// Performs the actual health check logic.
        /// </summary>
        /// <param name="context">The health check context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The health check result.</returns>
        protected abstract Task<HealthCheckResult> CheckHealthAsyncCore(
            HealthCheckContext context,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates a data dictionary with common health check information.
        /// </summary>
        /// <param name="additionalData">Additional data to include.</param>
        /// <returns>A dictionary with health check data.</returns>
        protected Dictionary<string, object> GetData(Dictionary<string, object>? additionalData = null)
        {
            var data = new Dictionary<string, object>
            {
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            return data;
        }
    }
}

