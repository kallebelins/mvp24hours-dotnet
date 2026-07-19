//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Mvp24Hours.Infrastructure.Http.Options;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class ResilienceTestHelpers
{
    public static RetryPolicyOptions CreateRetryOptions(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        BackoffType backoffType = BackoffType.Constant,
        double jitterFactor = 0,
        bool enabled = true,
        List<int>? retryStatusCodes = null)
    {
        return new RetryPolicyOptions
        {
            Enabled = enabled,
            MaxRetries = maxRetries,
            InitialDelay = initialDelay ?? TimeSpan.FromMilliseconds(1),
            MaxDelay = maxDelay ?? TimeSpan.FromMilliseconds(50),
            BackoffType = backoffType,
            JitterFactor = jitterFactor,
            RetryStatusCodes = retryStatusCodes ?? [408, 429, 500, 502, 503, 504]
        };
    }

    public static CircuitBreakerPolicyOptions CreateCircuitBreakerOptions(
        double failureRatio = 0.5,
        int minimumThroughput = 2,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null,
        bool enabled = true)
    {
        return new CircuitBreakerPolicyOptions
        {
            Enabled = enabled,
            FailureRatio = failureRatio,
            MinimumThroughput = minimumThroughput,
            SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30),
            BreakDuration = breakDuration ?? TimeSpan.FromSeconds(1)
        };
    }

    public static TimeoutPolicyOptions CreateTimeoutOptions(
        TimeSpan? timeout = null,
        bool enabled = true)
    {
        return new TimeoutPolicyOptions
        {
            Enabled = enabled,
            Timeout = timeout ?? TimeSpan.FromMilliseconds(100)
        };
    }

    public static Func<HttpRequestMessage> RequestFactory(string uri = "https://api.example.com/resource")
    {
        return () => new HttpRequestMessage(HttpMethod.Get, uri);
    }

    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> RespondWith(
        HttpStatusCode statusCode,
        string? content = null)
    {
        return (_, _) => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content ?? statusCode.ToString())
        });
    }

    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> RespondSequence(
        params HttpStatusCode[] statusCodes)
    {
        int index = 0;
        return (_, _) =>
        {
            HttpStatusCode status = statusCodes[Math.Min(index, statusCodes.Length - 1)];
            index++;
            return Task.FromResult(new HttpResponseMessage(status));
        };
    }

    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> FailThenSucceed(
        int failures,
        HttpStatusCode failureStatus = HttpStatusCode.InternalServerError,
        HttpStatusCode successStatus = HttpStatusCode.OK)
    {
        int attempts = 0;
        return (_, _) =>
        {
            attempts++;
            HttpStatusCode status = attempts <= failures ? failureStatus : successStatus;
            return Task.FromResult(new HttpResponseMessage(status));
        };
    }

    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> ThrowThenSucceed(
        int failures,
        Exception? exception = null)
    {
        int attempts = 0;
        return (_, _) =>
        {
            attempts++;
            if (attempts <= failures)
            {
                throw exception ?? new HttpRequestException("transient failure");
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };
    }

    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Delay(
        TimeSpan delay,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return async (_, ct) =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(statusCode);
        };
    }

    public static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Counting(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inner,
        Action<int> onAttempt)
    {
        int attempts = 0;
        return async (request, ct) =>
        {
            int current = Interlocked.Increment(ref attempts);
            onAttempt(current);
            return await inner(request, ct);
        };
    }
}
