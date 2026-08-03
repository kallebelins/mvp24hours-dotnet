using System.Threading.RateLimiting;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class RateLimitingFiltersCoverageTest
{
    [Fact]
    public async Task RateLimitingPublishFilter_WhenPermitAcquired_ShouldInvokeNext()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: true);
        var filter = new RateLimitingPublishFilter(provider.Object);
        PublishFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.PublishAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitingPublishFilter_WhenRateLimited_ShouldThrow()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: false);
        var filter = new RateLimitingPublishFilter(provider.Object);
        PublishFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());

        Func<Task> act = () => filter.PublishAsync(context, (_, _) => Task.CompletedTask);

        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    [Fact]
    public async Task RateLimitingPublishFilter_WithCustomKeyGenerator_ShouldUseGeneratedKey()
    {
        string? capturedKey = null;
        Mock<IRateLimiterProvider> provider = new();
        provider
            .Setup(p => p.AcquireAsync(
                It.IsAny<string>(),
                It.IsAny<NativeRateLimiterOptions>(),
                1,
                It.IsAny<CancellationToken>()))
            .Callback<string, NativeRateLimiterOptions, int, CancellationToken>((key, _, _, _) => capturedKey = key)
            .ReturnsAsync(CreateLease(acquired: true));

        var options = new RateLimitingPublishFilterOptions
        {
            KeyGenerator = (exchange, type) => $"custom_{exchange}_{type.Name}"
        };
        var filter = new RateLimitingPublishFilter(provider.Object, options);
        PublishFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent(), exchange: "orders");

        await filter.PublishAsync(context, (_, _) => Task.CompletedTask);

        capturedKey.Should().Be("custom_orders_TestOrderEvent");
    }

    [Fact]
    public async Task RateLimitingPublishFilter_WithKeyModeByRoutingKey_ShouldBuildRoutingKeyPartition()
    {
        string? capturedKey = null;
        Mock<IRateLimiterProvider> provider = new();
        provider
            .Setup(p => p.AcquireAsync(
                It.IsAny<string>(),
                It.IsAny<NativeRateLimiterOptions>(),
                1,
                It.IsAny<CancellationToken>()))
            .Callback<string, NativeRateLimiterOptions, int, CancellationToken>((key, _, _, _) => capturedKey = key)
            .ReturnsAsync(CreateLease(acquired: true));

        var options = new RateLimitingPublishFilterOptions
        {
            KeyMode = PublishRateLimitKeyMode.ByRoutingKey
        };
        var filter = new RateLimitingPublishFilter(provider.Object, options);
        PublishFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent(), routingKey: "route-1");

        await filter.PublishAsync(context, (_, _) => Task.CompletedTask);

        capturedKey.Should().Be("publish_routingkey_route-1");
    }

    [Fact]
    public async Task RateLimitingPublishFilter_WithNullProvider_ShouldThrow()
    {
        Action act = () => _ = new RateLimitingPublishFilter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task RateLimitingConsumeFilter_WhenPermitAcquired_ShouldInvokeNext()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: true);
        var filter = new RateLimitingConsumeFilter(provider.Object);
        ConsumeFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitingConsumeFilter_WhenExceededWithThrow_ShouldThrow()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: false);
        var options = new RateLimitingConsumeFilterOptions
        {
            ExceededBehavior = RateLimitExceededBehavior.Throw
        };
        var filter = new RateLimitingConsumeFilter(provider.Object, options);
        ConsumeFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => Task.CompletedTask);

        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    [Fact]
    public async Task RateLimitingConsumeFilter_WhenExceededWithRetry_ShouldSetRetry()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: false);
        var options = new RateLimitingConsumeFilterOptions
        {
            ExceededBehavior = RateLimitExceededBehavior.Retry,
            DefaultRetryDelay = TimeSpan.FromSeconds(2)
        };
        var filter = new RateLimitingConsumeFilter(provider.Object, options);
        ConsumeFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeFalse();
        context.RetryDelay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task RateLimitingConsumeFilter_WhenExceededWithDeadLetter_ShouldSendToDeadLetter()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: false);
        var options = new RateLimitingConsumeFilterOptions
        {
            ExceededBehavior = RateLimitExceededBehavior.DeadLetter
        };
        var filter = new RateLimitingConsumeFilter(provider.Object, options);
        ConsumeFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        await filter.ConsumeAsync(context, (_, _) => Task.CompletedTask);

        context.ShouldSendToDeadLetter.Should().BeTrue();
        context.DeadLetterReason.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task RateLimitingConsumeFilter_WhenExceededWithSkip_ShouldSkipRemainingFilters()
    {
        Mock<IRateLimiterProvider> provider = CreateProvider(acquired: false);
        var options = new RateLimitingConsumeFilterOptions
        {
            ExceededBehavior = RateLimitExceededBehavior.Skip
        };
        var filter = new RateLimitingConsumeFilter(provider.Object, options);
        ConsumeFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        await filter.ConsumeAsync(context, (_, _) => Task.CompletedTask);

        context.ShouldSkipRemainingFilters.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimitingConsumeFilter_WithTypeSpecificOptions_ShouldUseTypeOptions()
    {
        NativeRateLimiterOptions? capturedOptions = null;
        Mock<IRateLimiterProvider> provider = new();
        provider
            .Setup(p => p.AcquireAsync(
                It.IsAny<string>(),
                It.IsAny<NativeRateLimiterOptions>(),
                1,
                It.IsAny<CancellationToken>()))
            .Callback<string, NativeRateLimiterOptions, int, CancellationToken>((_, options, _, _) =>
                capturedOptions = options)
            .ReturnsAsync(CreateLease(acquired: true));

        var options = new RateLimitingConsumeFilterOptions();
        options.TypeSpecificOptions[typeof(TestOrderEvent)] = NativeRateLimiterOptions.FixedWindow(7);
        var filter = new RateLimitingConsumeFilter(provider.Object, options);
        ConsumeFilterContext<TestOrderEvent> context =
            RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        await filter.ConsumeAsync(context, (_, _) => Task.CompletedTask);

        capturedOptions!.PermitLimit.Should().Be(7);
    }

    [Fact]
    public void RateLimitingPublishFilterOptions_Default_ShouldExposeDefaults()
    {
        RateLimitingPublishFilterOptions options = RateLimitingPublishFilterOptions.Default;

        options.KeyMode.Should().Be(PublishRateLimitKeyMode.Global);
        options.DefaultRateLimiterOptions.Algorithm.Should().Be(RateLimitingAlgorithm.TokenBucket);
    }

    private static Mock<IRateLimiterProvider> CreateProvider(bool acquired)
    {
        Mock<IRateLimiterProvider> provider = new();
        provider
            .Setup(p => p.AcquireAsync(
                It.IsAny<string>(),
                It.IsAny<NativeRateLimiterOptions>(),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateLease(acquired));
        return provider;
    }

    private static RateLimitLease CreateLease(bool acquired)
    {
        Mock<RateLimitLease> lease = new();
        lease.SetupGet(l => l.IsAcquired).Returns(acquired);
        return lease.Object;
    }
}
