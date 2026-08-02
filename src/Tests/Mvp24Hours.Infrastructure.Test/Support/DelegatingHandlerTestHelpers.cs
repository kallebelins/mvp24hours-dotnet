//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Http.Options;
using Mvp24Hours.Infrastructure.Testing.Http;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class DelegatingHandlerTestHelpers
{
    public const string DefaultUri = "https://api.example.com/resource";

    public static ILogger<T> Logger<T>()
    {
        return NullLoggerFactory.Instance.CreateLogger<T>();
    }

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
        double failureRatio = 1.0,
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
            BreakDuration = breakDuration ?? TimeSpan.FromMilliseconds(200)
        };
    }

    public static HttpClient CreateClient(DelegatingHandler handler, HttpMessageHandler? inner = null)
    {
        handler.InnerHandler = inner ?? new TestHttpMessageHandler().RespondWith(HttpStatusCode.OK);
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };
    }

    public static TestHttpMessageHandler CreateSequenceHandler(params HttpStatusCode[] statusCodes)
    {
        int index = 0;
        var handler = new TestHttpMessageHandler();
        handler.When(
            _ => true,
            _ =>
            {
                HttpStatusCode status = statusCodes[Math.Min(index, statusCodes.Length - 1)];
                index++;
                return Task.FromResult(new HttpResponseMessage(status));
            });
        return handler;
    }

    public static TestHttpMessageHandler CreateFailThenSucceedHandler(
        int failures,
        HttpStatusCode failureStatus = HttpStatusCode.InternalServerError,
        HttpStatusCode successStatus = HttpStatusCode.OK)
    {
        int attempts = 0;
        var handler = new TestHttpMessageHandler();
        handler.When(
            _ => true,
            _ =>
            {
                attempts++;
                HttpStatusCode status = attempts <= failures ? failureStatus : successStatus;
                return Task.FromResult(new HttpResponseMessage(status));
            });
        return handler;
    }

    public static TestHttpMessageHandler CreateThrowThenSucceedHandler(int failures)
    {
        int attempts = 0;
        var handler = new TestHttpMessageHandler();
        handler.When(
            _ => true,
            _ =>
            {
                attempts++;
                if (attempts <= failures)
                {
                    throw new HttpRequestException("transient failure");
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            });
        return handler;
    }

    public static HttpMessageHandler CreateDelayedHandler(
        TimeSpan delay,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new CallbackMessageHandler(async (_, ct) =>
        {
            await Task.Delay(delay, ct);
            return new HttpResponseMessage(statusCode);
        });
    }

    public static HttpMessageHandler CreateConnectionRefusedHandler()
    {
        return new CallbackMessageHandler((_, _) =>
            throw new HttpRequestException(
                "Connection refused",
                new SocketException((int)SocketError.ConnectionRefused)));
    }

    public static HttpMessageHandler CreateCallbackHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback)
    {
        return new CallbackMessageHandler(callback);
    }

    public static IServiceProvider CreateServiceProviderWithHeaders(
        params (string Key, string Value)[] headers)
    {
        var httpContext = new DefaultHttpContext();
        foreach ((string key, string value) in headers)
        {
            httpContext.Request.Headers[key] = value;
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .BuildServiceProvider();
    }

    public static IServiceProvider CreateEmptyServiceProvider()
    {
        return new ServiceCollection().BuildServiceProvider();
    }

    public static IServiceProvider CreateThrowingServiceProvider()
    {
        var mock = new Moq.Mock<IServiceProvider>();
        mock.Setup(sp => sp.GetService(typeof(IHttpContextAccessor)))
            .Throws(new InvalidOperationException("service resolution failed"));
        return mock.Object;
    }

    private sealed class CallbackMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return callback(request, cancellationToken);
        }
    }
}
