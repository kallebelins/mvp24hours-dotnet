using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Infrastructure.RateLimiting;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Extensions;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class RateLimitingExtensionsCoverageTest
{
    [Fact]
    public void AddRabbitMQConsumerRateLimiting_ShouldRegisterProviderAndFilter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQConsumerRateLimiting(options =>
        {
            options.KeyMode = RateLimitKeyMode.ByMessageType;
            options.ExceededBehavior = RateLimitExceededBehavior.Throw;
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRateLimiterProvider>().Should().BeOfType<NativeRateLimiterProvider>();
        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is RateLimitingConsumeFilter);
    }

    [Fact]
    public void AddRabbitMQPublisherRateLimiting_ShouldRegisterProviderAndFilter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQPublisherRateLimiting(options => options.KeyMode = PublishRateLimitKeyMode.ByExchange);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRateLimiterProvider>().Should().BeOfType<NativeRateLimiterProvider>();
        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is RateLimitingPublishFilter);
    }

    [Fact]
    public void AddRabbitMQRateLimiting_ShouldRegisterBothFilters()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQRateLimiting(
            consumeOptions => consumeOptions.KeyMode = RateLimitKeyMode.Global,
            publishOptions => publishOptions.KeyMode = PublishRateLimitKeyMode.Global);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is RateLimitingConsumeFilter);
        provider.GetServices<IPublishFilter>().Should().ContainSingle(f => f is RateLimitingPublishFilter);
    }

    [Fact]
    public void AddRabbitMQConsumerRateLimitingSlidingWindow_ShouldConfigureSlidingWindow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQConsumerRateLimitingSlidingWindow(
            permitLimit: 50,
            window: TimeSpan.FromSeconds(2),
            segmentsPerWindow: 8,
            keyMode: RateLimitKeyMode.ByRoutingKey,
            exceededBehavior: RateLimitExceededBehavior.DeadLetter);

        ServiceProvider provider = services.BuildServiceProvider();
        RateLimitingConsumeFilter filter = provider.GetServices<IConsumeFilter>()
            .OfType<RateLimitingConsumeFilter>()
            .Single();

        filter.Should().NotBeNull();
        provider.GetRequiredService<IRateLimiterProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddRabbitMQConsumerRateLimitingTokenBucket_ShouldConfigureTokenBucket()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQConsumerRateLimitingTokenBucket(
            tokenLimit: 200,
            replenishmentPeriod: TimeSpan.FromMilliseconds(500),
            tokensPerPeriod: 20,
            keyMode: RateLimitKeyMode.ByConsumerTag,
            exceededBehavior: RateLimitExceededBehavior.Skip);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is RateLimitingConsumeFilter);
    }

    [Fact]
    public void AddRabbitMQConsumerRateLimitingConcurrency_ShouldConfigureConcurrencyLimiter()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQConsumerRateLimitingConcurrency(
            permitLimit: 5,
            queueLimit: 10,
            keyMode: RateLimitKeyMode.ByExchange,
            exceededBehavior: RateLimitExceededBehavior.Retry);

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IConsumeFilter>().Should().ContainSingle(f => f is RateLimitingConsumeFilter);
    }

    [Fact]
    public void AddRabbitMQConsumerRateLimiting_ShouldUseTryAddForProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQConsumerRateLimiting();
        services.AddRabbitMQPublisherRateLimiting();

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetServices<IRateLimiterProvider>().Should().ContainSingle();
    }
}
