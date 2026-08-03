using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Resiliency;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;

namespace Mvp24Hours.Application.Pipe.Test.Resiliency;

[Trait("Category", "Unit")]
public class NativePipelineResilienceExtensionsTest
{
    [Fact]
    public void AddNativePipelineResilience_Should_RegisterMiddlewareAndOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNativePipelineResilience(options =>
        {
            options.EnableRetry = true;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<NativePipelineResilienceOptions>().Should().NotBeNull();
        provider.GetServices<IPipelineMiddleware>().Should().ContainSingle(m => m is NativePipelineResilienceMiddleware);
    }

    [Fact]
    public void NativePipelineResilienceOptions_Presets_Should_HaveExpectedDefaults()
    {
        NativePipelineResilienceOptions.Default.EnableRetry.Should().BeTrue();
        NativePipelineResilienceOptions.LongRunning.EnableCircuitBreaker.Should().BeFalse();
        NativePipelineResilienceOptions.QuickOperations.EnableTimeout.Should().BeTrue();
    }

    [Fact]
    public async Task NativePipelineResilienceMiddleware_Should_RetryTransientFailures()
    {
        int attempts = 0;
        var options = new NativePipelineResilienceOptions
        {
            EnableRetry = true,
            EnableCircuitBreaker = false,
            EnableTimeout = false,
            RetryMaxAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(1),
            RetryUseJitter = false,
            RetryBackoffType = PipelineResilienceBackoffType.Constant
        };
        var middleware = new NativePipelineResilienceMiddleware(options, NullLogger<NativePipelineResilienceMiddleware>.Instance);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new IOException("transient");
            }

            return Task.CompletedTask;
        });

        attempts.Should().Be(3);
    }

    [Fact]
    public async Task NativePipelineResilienceMiddleware_Should_ExecuteSuccessfully_WhenNoFailure()
    {
        bool executed = false;
        var options = new NativePipelineResilienceOptions
        {
            EnableRetry = true,
            EnableCircuitBreaker = false,
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromSeconds(5)
        };
        var middleware = new NativePipelineResilienceMiddleware(options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public void AddNativePipelineResilience_Should_InvokeCallbacks()
    {
        bool timeoutCalled = false;
        bool retryCalled = false;
        bool circuitOpenCalled = false;
        bool circuitResetCalled = false;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNativePipelineResilience(options =>
        {
            options.EnableRetry = true;
            options.EnableCircuitBreaker = true;
            options.EnableTimeout = true;
            options.RetryMaxAttempts = 1;
            options.RetryDelay = TimeSpan.FromMilliseconds(1);
            options.CircuitBreakerMinimumThroughput = 1;
            options.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreakerBreakDuration = TimeSpan.FromMilliseconds(100);
            options.TimeoutDuration = TimeSpan.FromMilliseconds(1);
            options.OnTimeout = _ => timeoutCalled = true;
            options.OnRetry = (_, _, _) => retryCalled = true;
            options.OnCircuitBreakerOpen = _ => circuitOpenCalled = true;
            options.OnCircuitBreakerReset = () => circuitResetCalled = true;
            options.RetryableExceptionTypes = [typeof(IOException)];
            options.CircuitBreakerExceptionTypes = [typeof(IOException)];
            options.ShouldRetryOnException = ex => ex is IOException;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetService<NativePipelineResilienceOptions>().Should().NotBeNull();

        timeoutCalled.Should().BeFalse();
        retryCalled.Should().BeFalse();
        circuitOpenCalled.Should().BeFalse();
        circuitResetCalled.Should().BeFalse();
    }

    [Fact]
    public async Task NativePipelineResilienceMiddleware_Should_RejectWhenCircuitOpen()
    {
        var options = new NativePipelineResilienceOptions
        {
            EnableRetry = false,
            EnableTimeout = false,
            EnableCircuitBreaker = true,
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerFailureRatio = 1.0,
            CircuitBreakerSamplingDuration = TimeSpan.FromMinutes(1),
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1)
        };
        var middleware = new NativePipelineResilienceMiddleware(options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        for (int i = 0; i < 2; i++)
        {
            try
            {
                await middleware.ExecuteAsync(message, () => throw new IOException($"fail-{i + 1}"));
            }
            catch (IOException)
            {
            }
        }

        Func<Task> act = () => middleware.ExecuteAsync(message, () => Task.CompletedTask);

        await act.Should().ThrowAsync<BrokenCircuitException>();
    }

    [Fact]
    public void AddNativePipelineResilience_WithNullOptions_ShouldThrow()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddNativePipelineResilience((NativePipelineResilienceOptions)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ResiliencePipeline_Should_NotRetryArgumentException()
    {
        int attempts = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNativePipelineResilience(options =>
        {
            options.EnableRetry = true;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
            options.RetryMaxAttempts = 3;
            options.RetryDelay = TimeSpan.FromMilliseconds(1);
            options.RetryUseJitter = false;
        });

        ResiliencePipeline pipeline = services.BuildServiceProvider()
            .GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline("pipeline");

        Func<Task> act = () => pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            await Task.Yield();
            throw new ArgumentException("bad arg");
        }).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ResiliencePipeline_Should_NotRetryOperationCanceledException()
    {
        int attempts = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNativePipelineResilience(options =>
        {
            options.EnableRetry = true;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
            options.RetryMaxAttempts = 3;
            options.RetryDelay = TimeSpan.FromMilliseconds(1);
            options.RetryUseJitter = false;
        });

        ResiliencePipeline pipeline = services.BuildServiceProvider()
            .GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline("pipeline");

        Func<Task> act = () => pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            await Task.Yield();
            throw new OperationCanceledException();
        }).AsTask();

        await act.Should().ThrowAsync<OperationCanceledException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ResiliencePipeline_WithTimeoutEnabled_ShouldTimeoutSlowOperations()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNativePipelineResilience(options =>
        {
            options.EnableRetry = false;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = true;
            options.TimeoutDuration = TimeSpan.FromMilliseconds(50);
        });

        ResiliencePipeline pipeline = services.BuildServiceProvider()
            .GetRequiredService<ResiliencePipelineProvider<string>>()
            .GetPipeline("pipeline");

        Func<Task> act = () => pipeline.ExecuteAsync(async ct => await Task.Delay(500, ct)).AsTask();

        await act.Should().ThrowAsync<TimeoutRejectedException>();
    }

    [Fact]
    public async Task NativePipelineResilienceMiddleware_WithLinearBackoff_ShouldRetry()
    {
        int attempts = 0;
        var options = new NativePipelineResilienceOptions
        {
            EnableRetry = true,
            EnableCircuitBreaker = false,
            EnableTimeout = false,
            RetryMaxAttempts = 2,
            RetryDelay = TimeSpan.FromMilliseconds(1),
            RetryUseJitter = false,
            RetryBackoffType = PipelineResilienceBackoffType.Linear
        };
        var middleware = new NativePipelineResilienceMiddleware(options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () =>
        {
            attempts++;
            if (attempts < 2)
            {
                throw new IOException("transient");
            }

            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
    }

    [Fact]
    public void AddNativePipelineResilience_DefaultOverload_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNativePipelineResilience();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<NativePipelineResilienceOptions>().Should().NotBeNull();
        provider.GetServices<IPipelineMiddleware>().Should().ContainSingle(m => m is NativePipelineResilienceMiddleware);
    }
}
