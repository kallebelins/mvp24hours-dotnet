//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Mvp24Hours.WebAPI.HealthChecks;

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
/// <remarks>
/// Initializes a new instance of the <see cref="BaseHealthCheck"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public abstract class BaseHealthCheck(ILogger logger) : IHealthCheck
{
    /// <summary>
    /// Gets the logger instance.
    /// </summary>
    protected ILogger Logger { get; } = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string checkName = context.Registration.Name;
        DateTimeOffset startTime = DateTimeOffset.UtcNow;

        try
        {
            Logger.LogDebug("Starting health check: {CheckName}", checkName);

            HealthCheckResult result = await CheckHealthAsyncCore(context, cancellationToken);

            TimeSpan duration = DateTimeOffset.UtcNow - startTime;
            Logger.LogDebug(
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
            Logger.LogWarning("Health check cancelled: {CheckName}", checkName);
            throw;
        }
        catch (Exception ex)
        {
            TimeSpan duration = DateTimeOffset.UtcNow - startTime;
            Logger.LogError(
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
            foreach (KeyValuePair<string, object> kvp in additionalData)
            {
                data[kvp.Key] = kvp.Value;
            }
        }

        return data;
    }
}

