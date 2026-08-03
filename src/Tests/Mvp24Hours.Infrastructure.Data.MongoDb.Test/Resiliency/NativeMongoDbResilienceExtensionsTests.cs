//=====================================================================================
// Tests for NativeMongoDbResilienceExtensions (recommended replacement for the
// deprecated MongoDbResiliencyPolicy). Migrated from MongoDbResiliencyPolicyTests as
// part of task 4.3 (net10 warnings cleanup) to exercise the native Polly v8 pipeline
// via Microsoft.Extensions.Resilience instead of the obsolete API.
//=====================================================================================
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Polly;
using Polly.Registry;
using Polly.Timeout;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

[Trait("Category", "Unit")]
public class NativeMongoDbResilienceExtensionsTests
{
    private static ResiliencePipeline GetPipeline(IServiceCollection services, string name = "mongodb")
    {
        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline(name);
    }

    [Fact]
    public void Should_Register_Default_Pipeline_Resolvable_By_Name()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNativeMongoDbResilience();

        // Assert
        ResiliencePipeline pipeline = GetPipeline(services);
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Should_Register_Pipeline_With_Custom_Name()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddNativeMongoDbResilience("custom-mongo");

        // Assert
        ResiliencePipeline pipeline = GetPipeline(services, "custom-mongo");
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Should_Configure_Options_Via_Action()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert - configuration action is applied without throwing
        Action act = () => services.AddNativeMongoDbResilience(options =>
        {
            options.EnableRetry = true;
            options.RetryMaxAttempts = 5;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });

        act.Should().NotThrow();
        GetPipeline(services).Should().NotBeNull();
    }

    [Fact]
    public void Should_Throw_When_Options_Null()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Action act = () => services.AddNativeMongoDbResilience("mongodb", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Should_Execute_Operation_Through_Pipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNativeMongoDbResilience(options =>
        {
            options.EnableRetry = true;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });
        ResiliencePipeline pipeline = GetPipeline(services);

        // Act
        string result = await pipeline.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return "success";
        });

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task Should_Retry_Transient_Failure_And_Eventually_Succeed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNativeMongoDbResilience(options =>
        {
            options.EnableRetry = true;
            options.RetryMaxAttempts = 3;
            options.RetryBackoffType = MongoDbResilienceBackoffType.Constant;
            options.RetryDelay = TimeSpan.FromMilliseconds(1);
            options.RetryUseJitter = false;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });
        ResiliencePipeline pipeline = GetPipeline(services);
        int executionCount = 0;

        // Act
        string result = await pipeline.ExecuteAsync(async ct =>
        {
            executionCount++;
            await Task.Delay(1, ct);
            if (executionCount < 3)
            {
                throw new TimeoutException("Transient failure");
            }
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_Invoke_OnRetry_Callback()
    {
        // Arrange
        int retries = 0;
        var services = new ServiceCollection();
        services.AddNativeMongoDbResilience(options =>
        {
            options.EnableRetry = true;
            options.RetryMaxAttempts = 2;
            options.RetryBackoffType = MongoDbResilienceBackoffType.Constant;
            options.RetryDelay = TimeSpan.FromMilliseconds(1);
            options.RetryUseJitter = false;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
            options.OnRetry = (_, _, _) => retries++;
        });
        ResiliencePipeline pipeline = GetPipeline(services);

        // Act
        await Assert.ThrowsAsync<TimeoutException>(async () => await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(1, ct);
                throw new TimeoutException("Always fails");
            }));

        // Assert
        retries.Should().Be(2);
    }

    [Fact]
    public async Task Should_Enforce_Timeout()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNativeMongoDbResilience(options =>
        {
            options.EnableTimeout = true;
            options.TimeoutDuration = TimeSpan.FromMilliseconds(50);
            options.EnableRetry = false;
            options.EnableCircuitBreaker = false;
        });
        ResiliencePipeline pipeline = GetPipeline(services);

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutRejectedException>(async () => await pipeline.ExecuteAsync(async ct =>
            {
                await Task.Delay(5000, ct);
                return "should not complete";
            }));
    }

    [Fact]
    public async Task Should_Respect_Cancellation_Token()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddNativeMongoDbResilience(options =>
        {
            options.EnableRetry = false;
            options.EnableCircuitBreaker = false;
            options.EnableTimeout = false;
        });
        ResiliencePipeline pipeline = GetPipeline(services);
        using var cts = new CancellationTokenSource();
        var executionStarted = new TaskCompletionSource<bool>();

        // Act
        ValueTask<string> task = pipeline.ExecuteAsync(async ct =>
        {
            executionStarted.SetResult(true);
            await Task.Delay(10000, ct);
            return "should not complete";
        }, cts.Token);

        await executionStarted.Task;
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await task);
    }

    [Fact]
    public void Should_Provide_ReplicaSet_Preset()
    {
        NativeMongoDbResilienceOptions options = NativeMongoDbResilienceOptions.ReplicaSet;

        options.RetryMaxAttempts.Should().Be(5);
        options.EnableCircuitBreaker.Should().BeTrue();
        options.CircuitBreakerMinimumThroughput.Should().Be(10);
        options.EnableTimeout.Should().BeTrue();
    }

    [Fact]
    public void Should_Provide_ShardedCluster_Preset()
    {
        NativeMongoDbResilienceOptions options = NativeMongoDbResilienceOptions.ShardedCluster;

        options.RetryMaxAttempts.Should().Be(3);
        options.CircuitBreakerMinimumThroughput.Should().Be(20);
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Should_Provide_Standalone_Preset()
    {
        NativeMongoDbResilienceOptions options = NativeMongoDbResilienceOptions.Standalone;

        options.RetryMaxAttempts.Should().Be(3);
        options.CircuitBreakerMinimumThroughput.Should().Be(5);
        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(60));
    }
}
