using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Resiliency;

namespace Mvp24Hours.Application.Pipe.Test.Resiliency;

#pragma warning disable CS0618 // Legacy pipeline resiliency middleware retained for coverage until NativeResilience migration

[Trait("Category", "Unit")]
public class PipelineResiliencyExtensionsTest
{
    [Fact]
    public void AddMvpPipelineResiliency_WithDefaults_ShouldRegisterCoreMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineResiliency();

        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IPipelineMiddleware> middleware = provider.GetServices<IPipelineMiddleware>();

        middleware.Should().Contain(m => m is RetryPipelineMiddleware);
        middleware.Should().Contain(m => m is CircuitBreakerPipelineMiddleware);
        middleware.Should().Contain(m => m is DeadLetterPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineResiliency_WithDisabledFeatures_ShouldRegisterOnlyEnabledMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineResiliency(options =>
        {
            options.DisableAll();
            options.EnableFallback = true;
            options.EnableBulkhead = true;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        IEnumerable<IPipelineMiddleware> middleware = provider.GetServices<IPipelineMiddleware>();

        middleware.Should().Contain(m => m is FallbackPipelineMiddleware);
        middleware.Should().Contain(m => m is BulkheadPipelineMiddleware);
        middleware.Should().NotContain(m => m is RetryPipelineMiddleware);
        middleware.Should().NotContain(m => m is CircuitBreakerPipelineMiddleware);
        middleware.Should().NotContain(m => m is DeadLetterPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineRetry_WithNullOptions_ShouldUseDefaults()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineRetry();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<RetryOptions>().Should().NotBeNull();
        provider.GetServices<IPipelineMiddleware>().Should().ContainSingle(m => m is RetryPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineCircuitBreaker_ShouldRegisterTypedAndInterfaceMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineCircuitBreaker();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<CircuitBreakerPipelineMiddleware>().Should().NotBeNull();
        provider.GetServices<IPipelineMiddleware>().Should().Contain(m => m is CircuitBreakerPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineFallback_ShouldRegisterMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineFallback();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IPipelineMiddleware>().Should().ContainSingle(m => m is FallbackPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineBulkhead_ShouldRegisterMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineBulkhead();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<BulkheadPipelineMiddleware>().Should().NotBeNull();
        provider.GetServices<IPipelineMiddleware>().Should().Contain(m => m is BulkheadPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineDeadLetter_WithInMemoryStore_ShouldRegisterStoreAndMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpPipelineDeadLetter(storeType: DeadLetterStoreType.InMemory);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IDeadLetterStore>().Should().BeOfType<InMemoryDeadLetterStore>();
        provider.GetServices<IPipelineMiddleware>().Should().ContainSingle(m => m is DeadLetterPipelineMiddleware);
    }

    [Fact]
    public void AddMvpPipelineDeadLetterStore_ShouldRegisterCustomStore()
    {
        var services = new ServiceCollection();

        services.AddMvpPipelineDeadLetterStore<TestDeadLetterStore>();

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetService<IDeadLetterStore>().Should().BeOfType<TestDeadLetterStore>();
    }

    [Fact]
    public void PipelineResiliencyOptions_Presets_ShouldConfigurePolicies()
    {
        PipelineResiliencyOptions options = new PipelineResiliencyOptions()
            .UseAggressiveRetry()
            .UseConservativeRetry()
            .UseSensitiveCircuitBreaker()
            .UseTolerantCircuitBreaker()
            .ConfigureRetry(retry => retry.MaxRetryAttempts = 7)
            .ConfigureCircuitBreaker(cb => cb.FailureThreshold = 3)
            .ConfigureFallback(fb => fb.FallbackOnFaulty = true)
            .ConfigureBulkhead(bh => bh.MaxConcurrency = 2)
            .ConfigureDeadLetter(dl => dl.DeadLetterOnFaulty = true);

        options.RetryOptions.MaxRetryAttempts.Should().Be(7);
        options.CircuitBreakerOptions.FailureThreshold.Should().Be(3);
        options.FallbackOptions.FallbackOnFaulty.Should().BeTrue();
        options.EnableFallback.Should().BeTrue();
        options.EnableBulkhead.Should().BeTrue();
        options.BulkheadOptions.MaxConcurrency.Should().Be(2);
        options.DeadLetterOptions.DeadLetterOnFaulty.Should().BeTrue();
    }

    [Fact]
    public void PipelineResiliencyOptions_DisableAll_ShouldTurnOffEveryFeature()
    {
        PipelineResiliencyOptions options = new PipelineResiliencyOptions().DisableAll();

        options.EnableRetry.Should().BeFalse();
        options.EnableCircuitBreaker.Should().BeFalse();
        options.EnableFallback.Should().BeFalse();
        options.EnableBulkhead.Should().BeFalse();
        options.EnableDeadLetter.Should().BeFalse();
    }

    private sealed class TestDeadLetterStore : IDeadLetterStore
    {
        public Task StoreAsync(DeadLetterOperation deadLetter, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<DeadLetterOperation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<DeadLetterOperation?>(null);
        }

        public Task<IReadOnlyList<DeadLetterOperation>> GetAllAsync(
            string? operationName = null,
            DeadLetterReason? reason = null,
            DateTimeOffset? fromDate = null,
            DateTimeOffset? toDate = null,
            bool includeAcknowledged = false,
            int skip = 0,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DeadLetterOperation>>([]);
        }

        public Task<long> GetCountAsync(
            string? operationName = null,
            DeadLetterReason? reason = null,
            bool includeAcknowledged = false,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0L);
        }

        public Task<bool> AcknowledgeAsync(Guid id, string? acknowledgedBy = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> MarkReprocessedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<int> PurgeAcknowledgedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<IReadOnlyList<DeadLetterOperation>> GetForReprocessingAsync(
            int maxReprocessAttempts = 3,
            int take = 100,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DeadLetterOperation>>([]);
        }
    }
}

#pragma warning restore CS0618
