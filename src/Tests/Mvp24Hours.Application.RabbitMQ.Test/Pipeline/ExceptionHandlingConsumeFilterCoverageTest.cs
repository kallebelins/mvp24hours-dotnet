using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class ExceptionHandlingConsumeFilterCoverageTest
{
    private static ExceptionHandlingConsumeFilter CreateFilter(ExceptionHandlingFilterOptions options)
    {
        return new ExceptionHandlingConsumeFilter(options: Options.Create(options));
    }

    [Fact]
    public async Task ConsumeAsync_WhenRedeliveryCountBelowMaxRetries_ShouldSetRetryOnContext()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 3,
            UseExponentialBackoff = false,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            RethrowException = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(1));

        await filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        context.ShouldRetry.Should().BeTrue();
        context.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(200));
        context.ShouldSendToDeadLetter.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeAsync_WhenRedeliveryCountReachesMaxRetries_ShouldSendToDeadLetter()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 2,
            RethrowException = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(2));

        await filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        context.ShouldSendToDeadLetter.Should().BeTrue();
        context.DeadLetterReason.Should().Contain("Max retries (2) exceeded");
        context.DeadLetterReason.Should().Contain("boom");
        context.ShouldRetry.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeAsync_WithExponentialBackoff_ShouldDoubleDelayPerRedelivery()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 5,
            UseExponentialBackoff = true,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            MaxRetryDelay = TimeSpan.FromSeconds(30),
            AddJitter = false,
            RethrowException = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(2));

        await filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        // baseDelay(100ms) * 2^2 = 400ms
        context.RetryDelay.Should().Be(TimeSpan.FromMilliseconds(400));
    }

    [Fact]
    public async Task ConsumeAsync_WithExponentialBackoffExceedingCap_ShouldClampToMaxRetryDelay()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 10,
            UseExponentialBackoff = true,
            RetryDelay = TimeSpan.FromSeconds(1),
            MaxRetryDelay = TimeSpan.FromSeconds(5),
            AddJitter = false,
            RethrowException = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(8));

        await filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        context.RetryDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ConsumeAsync_WithJitter_ShouldAddDelayWithinExpectedBounds()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 5,
            UseExponentialBackoff = false,
            RetryDelay = TimeSpan.FromMilliseconds(1000),
            AddJitter = true,
            JitterFactor = 0.1,
            RethrowException = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(1));

        await filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        context.RetryDelay!.Value.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(1000);
        context.RetryDelay!.Value.TotalMilliseconds.Should().BeLessThanOrEqualTo(1100);
    }

    [Fact]
    public async Task ConsumeAsync_WhenExceptionInIgnoreList_ShouldSkipRetryAndPropagateAsUnhandled()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 3,
            ExceptionsToIgnore = [typeof(ArgumentException)],
            SendUnhandledToDeadLetter = true
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(0));

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => throw new ArgumentException("bad arg"));

        await act.Should().ThrowAsync<ArgumentException>();
        context.ShouldRetry.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeTrue();
        context.DeadLetterReason.Should().Contain("Unhandled exception type");
    }

    [Fact]
    public async Task ConsumeAsync_WhenExceptionsToHandleConfiguredAndMatches_ShouldHandleNormally()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 3,
            ExceptionsToHandle = [typeof(InvalidOperationException)],
            RethrowException = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(0));

        await filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("handled"));

        context.ShouldRetry.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeAsync_WhenExceptionsToHandleConfiguredAndDoesNotMatch_ShouldPropagateAsUnhandled()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 3,
            ExceptionsToHandle = [typeof(InvalidOperationException)],
            SendUnhandledToDeadLetter = true
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(0));

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => throw new NotSupportedException("not in list"));

        await act.Should().ThrowAsync<NotSupportedException>();
        context.ShouldSendToDeadLetter.Should().BeTrue();
    }

    [Fact]
    public async Task ConsumeAsync_WhenOperationCanceled_ShouldAlwaysPropagateAsUnhandled()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 3,
            SendUnhandledToDeadLetter = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(0));

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => throw new OperationCanceledException());

        await act.Should().ThrowAsync<OperationCanceledException>();
        context.ShouldRetry.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeAsync_WhenUnhandledAndSendUnhandledToDeadLetterDisabled_ShouldNotSendToDeadLetter()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions
        {
            MaxRetries = 3,
            ExceptionsToIgnore = [typeof(ArgumentException)],
            SendUnhandledToDeadLetter = false
        });
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithRedeliveryCount(0));

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => throw new ArgumentException("bad"));

        await act.Should().ThrowAsync<ArgumentException>();
        context.ShouldSendToDeadLetter.Should().BeFalse();
    }

    [Fact]
    public async Task ConsumeAsync_WhenSuccessful_ShouldNotSetExceptionOrRetryFlags()
    {
        var filter = CreateFilter(new ExceptionHandlingFilterOptions());
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool called = false;

        await filter.ConsumeAsync(context, (_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        called.Should().BeTrue();
        context.ShouldRetry.Should().BeFalse();
        context.ShouldSendToDeadLetter.Should().BeFalse();
        context.Exception.Should().BeNull();
    }
}
