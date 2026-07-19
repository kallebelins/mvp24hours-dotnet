using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;

namespace Mvp24Hours.Application.RabbitMQ.Test.Configuration;

public class ConfigurationTest
{
    [Fact]
    public void BatchConsumerOptions_Default_ShouldHaveExpectedValues()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;

        options.MaxBatchSize.Should().Be(10);
        options.MinBatchSize.Should().Be(1);
        options.PrefetchCount.Should().BeGreaterThanOrEqualTo((ushort)options.MaxBatchSize);
    }

    [Fact]
    public void BatchConsumerOptions_Validate_WithInvalidSizes_ShouldThrow()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 0 };

        Action act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MaxBatchSize*");
    }

    [Fact]
    public void BatchConsumerOptions_Validate_WithMinGreaterThanMax_ShouldThrow()
    {
        var options = new BatchConsumerOptions { MinBatchSize = 5, MaxBatchSize = 2 };

        Action act = () => options.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*MinBatchSize*");
    }

    [Fact]
    public void BatchConsumerOptions_HighThroughputPreset_ShouldEnableParallelProcessing()
    {
        BatchConsumerOptions options = BatchConsumerOptions.HighThroughput;

        options.EnableParallelProcessing.Should().BeTrue();
        options.MaxBatchSize.Should().Be(100);
    }

    [Fact]
    public void RabbitMQClientOptions_ShouldHaveFeatureDefaults()
    {
        var options = new RabbitMQClientOptions();

        options.MaxRedeliveredCount.Should().Be(3);
        options.Deduplication.Enabled.Should().BeFalse();
        options.PublisherConfirm.Enabled.Should().BeTrue();
        options.PriorityQueue.Enabled.Should().BeFalse();
    }

    [Fact]
    public void MessageDeduplicationOptions_ShouldDefaultDisabled()
    {
        var options = new MessageDeduplicationOptions();

        options.Enabled.Should().BeFalse();
        options.MessageIdHeaderName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void RequestClientOptions_ShouldDefaultTimeout()
    {
        var options = new RequestClientOptions();

        options.TimeoutMilliseconds.Should().BeGreaterThan(0);
        options.Exchange.Should().NotBeNull();
    }

    [Fact]
    public void MessageSchedulerOptions_ShouldHavePollingDefaults()
    {
        var options = new MessageSchedulerOptions();

        options.PollingInterval.Should().BeGreaterThan(TimeSpan.Zero);
        options.BatchSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RabbitMQHostedOptions_ShouldRequireCallback()
    {
        var options = new RabbitMQHostedOptions { Callback = _ => { } };

        options.Callback.Should().NotBeNull();
        options.Period.Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void PublisherConfirmOptions_ShouldDefaultToEnabled()
    {
        var options = new PublisherConfirmOptions();

        options.Enabled.Should().BeTrue();
        options.WaitForConfirmsOrDie.Should().BeTrue();
        options.TimeoutMilliseconds.Should().Be(5000);
    }

    [Fact]
    public void ConsumerPrefetchOptions_ShouldHaveQoSDefaults()
    {
        var options = new ConsumerPrefetchOptions();

        options.PrefetchCount.Should().BeGreaterThan(0);
        options.Global.Should().BeFalse();
    }

    [Fact]
    public void BatchPublishOptions_ShouldHaveBatchDefaults()
    {
        var options = new BatchPublishOptions();

        options.Enabled.Should().BeFalse();
        options.MaxBatchSize.Should().BeGreaterThan(0);
    }
}
