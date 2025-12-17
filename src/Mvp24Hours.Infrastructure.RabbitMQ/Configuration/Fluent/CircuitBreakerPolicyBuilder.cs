//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Configuration.Fluent
{
    /// <summary>
    /// Builder for configuring circuit breaker policies for message handling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The circuit breaker pattern prevents an application from repeatedly trying
    /// to execute an operation that's likely to fail. It allows the application
    /// to continue without waiting for the fault to be fixed or wasting resources
    /// while it determines that the fault is long-lasting.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// cfg.UseCircuitBreaker(cb =>
    /// {
    ///     cb.TrackingPeriod(TimeSpan.FromMinutes(1));
    ///     cb.TripThreshold(15);
    ///     cb.ActiveThreshold(10);
    ///     cb.ResetInterval(TimeSpan.FromMinutes(5));
    /// });
    /// </code>
    /// </example>
    public class CircuitBreakerPolicyBuilder
    {
        private readonly CircuitBreakerPolicyConfiguration _configuration = new();

        /// <summary>
        /// Sets the time period over which failures are tracked.
        /// Default is 1 minute.
        /// </summary>
        /// <param name="period">The tracking period.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cb.TrackingPeriod(TimeSpan.FromMinutes(2));
        /// </code>
        /// </example>
        public CircuitBreakerPolicyBuilder TrackingPeriod(TimeSpan period)
        {
            _configuration.TrackingPeriod = period;
            return this;
        }

        /// <summary>
        /// Sets the number of failures required to trip the circuit breaker.
        /// Default is 15.
        /// </summary>
        /// <param name="threshold">The number of failures before tripping.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cb.TripThreshold(20);
        /// </code>
        /// </example>
        public CircuitBreakerPolicyBuilder TripThreshold(int threshold)
        {
            _configuration.TripThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Sets the minimum number of requests required before the circuit can trip.
        /// Default is 10.
        /// </summary>
        /// <param name="threshold">The minimum number of active requests.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This prevents the circuit from tripping due to sporadic failures
        /// when there's very little traffic.
        /// </para>
        /// </remarks>
        public CircuitBreakerPolicyBuilder ActiveThreshold(int threshold)
        {
            _configuration.ActiveThreshold = threshold;
            return this;
        }

        /// <summary>
        /// Sets the time to wait before attempting to reset the circuit.
        /// Default is 5 minutes.
        /// </summary>
        /// <param name="interval">The reset interval.</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cb.ResetInterval(TimeSpan.FromMinutes(10));
        /// </code>
        /// </example>
        public CircuitBreakerPolicyBuilder ResetInterval(TimeSpan interval)
        {
            _configuration.ResetInterval = interval;
            return this;
        }

        /// <summary>
        /// Sets the failure rate threshold (percentage) to trip the circuit.
        /// Default is 50%.
        /// </summary>
        /// <param name="percentage">The failure rate threshold (0-100).</param>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cb.FailureRateThreshold(60); // Trip when 60% of requests fail
        /// </code>
        /// </example>
        public CircuitBreakerPolicyBuilder FailureRateThreshold(int percentage)
        {
            _configuration.FailureRateThreshold = Math.Clamp(percentage, 0, 100);
            return this;
        }

        /// <summary>
        /// Sets the duration of the half-open state.
        /// Default is 30 seconds.
        /// </summary>
        /// <param name="duration">The half-open duration.</param>
        /// <returns>The builder for chaining.</returns>
        /// <remarks>
        /// <para>
        /// In the half-open state, a limited number of requests are allowed through
        /// to test if the underlying issue has been resolved.
        /// </para>
        /// </remarks>
        public CircuitBreakerPolicyBuilder HalfOpenDuration(TimeSpan duration)
        {
            _configuration.HalfOpenDuration = duration;
            return this;
        }

        /// <summary>
        /// Sets the number of successful requests required to close the circuit.
        /// Default is 3.
        /// </summary>
        /// <param name="count">The number of consecutive successes required.</param>
        /// <returns>The builder for chaining.</returns>
        public CircuitBreakerPolicyBuilder SuccessThreshold(int count)
        {
            _configuration.SuccessThreshold = count;
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker to trip only for specific exception types.
        /// </summary>
        /// <typeparam name="TException">The exception type to handle.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cb.Handle&lt;TimeoutException&gt;()
        ///   .Handle&lt;TransientException&gt;();
        /// </code>
        /// </example>
        public CircuitBreakerPolicyBuilder Handle<TException>() where TException : Exception
        {
            _configuration.HandledExceptions.Add(typeof(TException));
            return this;
        }

        /// <summary>
        /// Configures the circuit breaker to ignore specific exception types.
        /// </summary>
        /// <typeparam name="TException">The exception type to ignore.</typeparam>
        /// <returns>The builder for chaining.</returns>
        /// <example>
        /// <code>
        /// cb.Ignore&lt;ValidationException&gt;();
        /// </code>
        /// </example>
        public CircuitBreakerPolicyBuilder Ignore<TException>() where TException : Exception
        {
            _configuration.IgnoredExceptions.Add(typeof(TException));
            return this;
        }

        /// <summary>
        /// Sets a callback to be invoked when the circuit breaks (opens).
        /// </summary>
        /// <param name="onBreak">The callback action.</param>
        /// <returns>The builder for chaining.</returns>
        public CircuitBreakerPolicyBuilder OnBreak(Action<Exception, TimeSpan>? onBreak)
        {
            _configuration.OnBreak = onBreak;
            return this;
        }

        /// <summary>
        /// Sets a callback to be invoked when the circuit resets (closes).
        /// </summary>
        /// <param name="onReset">The callback action.</param>
        /// <returns>The builder for chaining.</returns>
        public CircuitBreakerPolicyBuilder OnReset(Action? onReset)
        {
            _configuration.OnReset = onReset;
            return this;
        }

        /// <summary>
        /// Sets a callback to be invoked when the circuit enters half-open state.
        /// </summary>
        /// <param name="onHalfOpen">The callback action.</param>
        /// <returns>The builder for chaining.</returns>
        public CircuitBreakerPolicyBuilder OnHalfOpen(Action? onHalfOpen)
        {
            _configuration.OnHalfOpen = onHalfOpen;
            return this;
        }

        /// <summary>
        /// Builds the circuit breaker policy configuration.
        /// </summary>
        /// <returns>The circuit breaker policy configuration.</returns>
        internal CircuitBreakerPolicyConfiguration Build()
        {
            return _configuration;
        }
    }

    /// <summary>
    /// Configuration for circuit breaker policies.
    /// </summary>
    public class CircuitBreakerPolicyConfiguration
    {
        /// <summary>
        /// Gets or sets the tracking period for failures.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan TrackingPeriod { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Gets or sets the number of failures required to trip the circuit.
        /// Default is 15.
        /// </summary>
        public int TripThreshold { get; set; } = 15;

        /// <summary>
        /// Gets or sets the minimum number of requests before tripping.
        /// Default is 10.
        /// </summary>
        public int ActiveThreshold { get; set; } = 10;

        /// <summary>
        /// Gets or sets the time to wait before attempting to reset.
        /// Default is 5 minutes.
        /// </summary>
        public TimeSpan ResetInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the failure rate threshold (percentage).
        /// Default is 50%.
        /// </summary>
        public int FailureRateThreshold { get; set; } = 50;

        /// <summary>
        /// Gets or sets the duration of the half-open state.
        /// Default is 30 seconds.
        /// </summary>
        public TimeSpan HalfOpenDuration { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the number of consecutive successes required to close the circuit.
        /// Default is 3.
        /// </summary>
        public int SuccessThreshold { get; set; } = 3;

        /// <summary>
        /// Gets the exception types to handle (count as failures).
        /// </summary>
        public HashSet<Type> HandledExceptions { get; } = new();

        /// <summary>
        /// Gets the exception types to ignore (don't count as failures).
        /// </summary>
        public HashSet<Type> IgnoredExceptions { get; } = new();

        /// <summary>
        /// Gets or sets the callback invoked when the circuit breaks.
        /// </summary>
        public Action<Exception, TimeSpan>? OnBreak { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the circuit resets.
        /// </summary>
        public Action? OnReset { get; set; }

        /// <summary>
        /// Gets or sets the callback invoked when the circuit enters half-open.
        /// </summary>
        public Action? OnHalfOpen { get; set; }

        /// <summary>
        /// Determines whether the given exception should count as a failure.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception should count as a failure.</returns>
        public bool ShouldCount(Exception exception)
        {
            var exceptionType = exception.GetType();

            // If we have ignored exceptions and this is one, don't count it
            if (IgnoredExceptions.Count > 0)
            {
                foreach (var ignored in IgnoredExceptions)
                {
                    if (ignored.IsAssignableFrom(exceptionType))
                        return false;
                }
            }

            // If we have handled exceptions specified, only count those
            if (HandledExceptions.Count > 0)
            {
                foreach (var handled in HandledExceptions)
                {
                    if (handled.IsAssignableFrom(exceptionType))
                        return true;
                }
                return false;
            }

            // Default: count all exceptions
            return true;
        }
    }

    /// <summary>
    /// Circuit breaker states.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit is closed - requests pass through normally.
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open - requests are immediately rejected.
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open - limited requests are allowed to test if the issue is resolved.
        /// </summary>
        HalfOpen
    }
}

