//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
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
    public static class HttpPolicyHelper
    {
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
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-retry-unauthorized-policy-onretry", $"correlation-id: {context.CorrelationId}|messsage: Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to {exception}.");
                        if (action != null)
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-retry-unauthorized-policy-action", $"correlation-id: {context.CorrelationId}|messsage: Retry {retryCount} of {context.PolicyKey} at {context.OperationKey}, due to {exception}.");
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
                        TelemetryHelper.Execute(TelemetryLevels.Error, "polly-get-circuit-breaker-policy-onbreak-failure", exception, $"messsage: Circuit cut, request will not flow in {sleepDuration}.");
                    }),
                    onReset: () => Task.Run(() =>
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onreset", $"messsage: Circuit closed, request flow normally.");
                    }),
                    onHalfOpen: () => Task.Run(() =>
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onhalfopen", $"messsage: Circuit in test mode, one request will be allowed.");
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
                        TelemetryHelper.Execute(TelemetryLevels.Error, "polly-get-circuit-breaker-policy-onbreak-failure", exception, $"messsage: Circuit cut, request will not flow in {sleepDuration}.");
                        if (action != null)
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onbreak-action");
                            action("onBreak");
                        }
                    }),
                    onReset: () => Task.Run(() =>
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onreset", $"messsage: Circuit closed, request flow normally.");
                        if (action != null)
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onreset-action");
                            action("onReset");
                        }
                    }),
                    onHalfOpen: () => Task.Run(() =>
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onhalfopen", $"messsage: Circuit in test mode, one request will be allowed.");
                        if (action != null)
                        {
                            TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-circuit-breaker-policy-onhalfopen-action");
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
                    TelemetryHelper.Execute(TelemetryLevels.Error, "polly-get-timeout-policy-failure", exception);
                    if (action != null)
                    {
                        TelemetryHelper.Execute(TelemetryLevels.Verbose, "polly-get-timeout-policy-action", $"correlation-id:{context.CorrelationId}|exception:{exception}");
                        action(context.CorrelationId);
                    }
                })
            );
        }
    }
}
