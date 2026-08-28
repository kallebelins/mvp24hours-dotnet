//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that should use circuit breaker protection.
/// </summary>
/// <remarks>
/// <para>
/// Requests implementing this interface will have circuit breaker protection applied.
/// When too many failures occur, the circuit will "open" and reject requests immediately.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ExternalApiCommand : IMediatorCommand&lt;string&gt;, ICircuitBreakerProtected
/// {
///     public string CircuitBreakerKey => "external-api";
///     public int FailureThreshold => 5;
///     public int SamplingDurationSeconds => 30;
///     public int MinimumThroughput => 10;
///     public int DurationOfBreakSeconds => 60;
/// }
/// </code>
/// </example>
public interface ICircuitBreakerProtected
{
    /// <summary>
    /// Gets the unique key for this circuit breaker.
    /// Requests with the same key share the same circuit state.
    /// If null, the request type name is used.
    /// </summary>
    string? CircuitBreakerKey => null;

    /// <summary>
    /// Gets the number of failures before the circuit opens.
    /// Default is 5.
    /// </summary>
    int FailureThreshold => 5;

    /// <summary>
    /// Gets the duration in seconds for the sampling window.
    /// Failures are counted within this window.
    /// Default is 30 seconds.
    /// </summary>
    int SamplingDurationSeconds => 30;

    /// <summary>
    /// Gets the minimum number of requests before the circuit can trip.
    /// Default is 10.
    /// </summary>
    int MinimumThroughput => 10;

    /// <summary>
    /// Gets the duration in seconds the circuit stays open before allowing a test request.
    /// Default is 60 seconds.
    /// </summary>
    int DurationOfBreakSeconds => 60;
}
