//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Helpers
{
    /// <summary>
    /// Helper class for creating HTTP resilience policies using Polly.
    /// </summary>
    public static class HttpPolicyHelper
    {
        private static ILogger _logger;

        /// <summary>
        /// Sets the logger instance for logging policy operations.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Allows configuring automatic retries.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpStatusCode statusCode, int numberOfAttempts = 3)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == statusCode)
                .RetryAsync(numberOfAttempts);
        }

        /// <summary>
        /// Allows configuring automatic retries.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(HttpStatusCode statusCode, Action<Guid> action, int numberOfAttempts = 3, int sleepDuration = 2)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == statusCode)
                .WaitAndRetryAsync(
                    retryCount: numberOfAttempts,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(sleepDuration, retryAttempt)),
                    onRetry: (exception, retryCount, context) =>
                    {
                        _logger?.LogWarning("Retry {RetryCount} of {PolicyKey} at {OperationKey} due to {ExceptionType}. CorrelationId: {CorrelationId}",
                            retryCount, context.PolicyKey, context.OperationKey, exception?.GetType().Name, context.CorrelationId);
                        if (action != null)
                        {
                            _logger?.LogDebug("Executing retry action for CorrelationId: {CorrelationId}", context.CorrelationId);
                            action(context.CorrelationId);
                        }
                    });
        }

        /// <summary>
        /// Breaks the circuit (blocks executions) for a period, when faults exceed some pre-configured threshold.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(HttpStatusCode statusCode, int eventsBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == statusCode)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: eventsBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (exception, sleepDuration) => Task.Run(() =>
                    {
                        _logger?.LogError(exception, "Circuit breaker opened. Requests will not flow for {Duration}. Policy: {PolicyKey}",
                            sleepDuration, exception?.GetType().Name);
                    }),
                    onReset: () => Task.Run(() =>
                    {
                        _logger?.LogInformation("Circuit breaker closed. Requests will flow normally.");
                    }),
                    onHalfOpen: () => Task.Run(() =>
                    {
                        _logger?.LogInformation("Circuit breaker half-open. One test request will be allowed.");
                    })
                );
        }

        /// <summary>
        /// Breaks the circuit (blocks executions) for a period, when faults exceed some pre-configured threshold.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(HttpStatusCode statusCode, Action<string> action, int eventsBeforeBreaking = 5, int durationOfBreakInSeconds = 30)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == statusCode)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: eventsBeforeBreaking,
                    durationOfBreak: TimeSpan.FromSeconds(durationOfBreakInSeconds),
                    onBreak: (exception, sleepDuration) => Task.Run(() =>
                    {
                        _logger?.LogError(exception, "Circuit breaker opened. Requests will not flow for {Duration}",
                            sleepDuration);
                        if (action != null)
                        {
                            _logger?.LogDebug("Executing onBreak action");
                            action("onBreak");
                        }
                    }),
                    onReset: () => Task.Run(() =>
                    {
                        _logger?.LogInformation("Circuit breaker closed. Requests will flow normally.");
                        if (action != null)
                        {
                            _logger?.LogDebug("Executing onReset action");
                            action("onReset");
                        }
                    }),
                    onHalfOpen: () => Task.Run(() =>
                    {
                        _logger?.LogInformation("Circuit breaker half-open. One test request will be allowed.");
                        if (action != null)
                        {
                            _logger?.LogDebug("Executing onHalfOpen action");
                            action("onHalfOpen");
                        }
                    })
                );
        }

        /// <summary>
        /// Defines an alternative value to be returned (or action to be executed) on failure.
        /// </summary>
        /// <param name="action"></param>
        /// <example>
        ///     private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        ///     {
        ///         Console.WriteLine("Fallback action is executing");
        ///     
        ///         HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        ///         {
        ///             Content = new StringContent(JsonSerializer.Serialize(new ResponseModel("Deu bom", true)))
        ///         };
        ///         return Task.FromResult(httpResponseMessage);
        ///     }
        /// </example>
        /// <returns></returns>
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackBrokenCircuitPolicy(Func<CancellationToken, Task<HttpResponseMessage>> fallbackAction)
        {
            return Policy<HttpResponseMessage>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(fallbackAction);
        }

        /// <summary>
        /// Defines an alternative value to be returned (or action to be executed) on failure.
        /// </summary>
        /// <param name="action"></param>
        /// <example>
        ///     private Task OnFallbackAsync(DelegateResult<HttpResponseMessage> response, Context context)
        ///     {
        ///         Console.WriteLine("About to call the fallback action. This is a good place to do some logging");
        ///         return Task.CompletedTask;
        ///     }
        ///     private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        ///     {
        ///         Console.WriteLine("Fallback action is executing");
        ///     
        ///         HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        ///         {
        ///             Content = new StringContent(JsonSerializer.Serialize(new ResponseModel("Deu bom", true)))
        ///         };
        ///         return Task.FromResult(httpResponseMessage);
        ///     }
        /// </example>
        /// <returns></returns>
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackBrokenCircuitPolicy(Func<CancellationToken, Task<HttpResponseMessage>> fallbackAction, Func<DelegateResult<HttpResponseMessage>, Task> onFallbackAsync)
        {
            return Policy<HttpResponseMessage>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(fallbackAction, onFallbackAsync);
        }

        /// <summary>
        /// Defines an alternative value to be returned (or action to be executed) on failure.
        /// </summary>
        /// <param name="action"></param>
        /// <example>
        ///     private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        ///     {
        ///         Console.WriteLine("Fallback action is executing");
        ///     
        ///         HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        ///         {
        ///             Content = new StringContent(JsonSerializer.Serialize(new ResponseModel("Deu bom", true)))
        ///         };
        ///         return Task.FromResult(httpResponseMessage);
        ///     }
        /// </example>
        /// <returns></returns>
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(HttpStatusCode statusCode, Func<CancellationToken, Task<HttpResponseMessage>> fallbackAction)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == statusCode)
                .FallbackAsync(fallbackAction);
        }

        /// <summary>
        /// Defines an alternative value to be returned (or action to be executed) on failure.
        /// </summary>
        /// <param name="action"></param>
        /// <example>
        ///     private Task OnFallbackAsync(DelegateResult<HttpResponseMessage> response, Context context)
        ///     {
        ///         Console.WriteLine("About to call the fallback action. This is a good place to do some logging");
        ///         return Task.CompletedTask;
        ///     }
        ///     private Task<HttpResponseMessage> FallbackAction(DelegateResult<HttpResponseMessage> responseToFailedRequest, Context context, CancellationToken cancellationToken)
        ///     {
        ///         Console.WriteLine("Fallback action is executing");
        ///     
        ///         HttpResponseMessage httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        ///         {
        ///             Content = new StringContent(JsonSerializer.Serialize(new ResponseModel("Deu bom", true)))
        ///         };
        ///         return Task.FromResult(httpResponseMessage);
        ///     }
        /// </example>
        /// <returns></returns>
        public static IAsyncPolicy<HttpResponseMessage> GetFallbackPolicy(HttpStatusCode statusCode, Func<CancellationToken, Task<HttpResponseMessage>> fallbackAction, Func<DelegateResult<HttpResponseMessage>, Task> onFallbackAsync)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == statusCode)
                .FallbackAsync(fallbackAction, onFallbackAsync);
        }

        /// <summary>
        /// Guarantees the caller won't have to wait beyond the timeout.
        /// </summary>
        public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(Action<Guid> action = null, int timeoutInSeconds = 300)
        {
            return Policy
                .TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(timeoutInSeconds),
                timeoutStrategy: TimeoutStrategy.Optimistic,
                onTimeoutAsync: (context, sleepDuration, task, exception) => Task.Run(() =>
                {
                    _logger?.LogError(exception, "Request timeout after {Timeout} seconds. CorrelationId: {CorrelationId}",
                        sleepDuration.TotalSeconds, context.CorrelationId);
                    if (action != null)
                    {
                        _logger?.LogDebug("Executing timeout action for CorrelationId: {CorrelationId}", context.CorrelationId);
                        action(context.CorrelationId);
                    }
                })
            );
        }
    }
}
