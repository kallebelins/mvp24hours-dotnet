using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Channels;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;

namespace Mvp24Hours.Application.RabbitMQ.Test.Channels;

public class ChannelBatchProcessorTest
{
    [Fact]
    public void ChannelBatchProcessorOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new ChannelBatchProcessorOptions();

        options.MaxBatchSize.Should().Be(100);
        options.MinBatchSize.Should().Be(1);
        options.BatchTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.ChannelCapacity.Should().Be(200);
        options.RequeueOnFailure.Should().BeTrue();
    }

    [Fact]
    public void ChannelBatchProcessorOptions_CustomValues_ShouldBeSetCorrectly()
    {
        var options = new ChannelBatchProcessorOptions
        {
            MaxBatchSize = 50,
            MinBatchSize = 5,
            BatchTimeout = TimeSpan.FromSeconds(10),
            ChannelCapacity = 100,
            RequeueOnFailure = false
        };

        options.MaxBatchSize.Should().Be(50);
        options.MinBatchSize.Should().Be(5);
        options.BatchTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.ChannelCapacity.Should().Be(100);
        options.RequeueOnFailure.Should().BeFalse();
    }

    [Fact]
    public void BatchConsumerOptions_ChannelProcessor_ValidOptions_ShouldValidate()
    {
        var options = new BatchConsumerOptions
        {
            MaxBatchSize = 10,
            MinBatchSize = 1
        };

        Action act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void BatchConsumerOptions_PrefetchCount_ShouldBeAtLeastMaxBatchSize()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 20 };

        options.PrefetchCount.Should().BeGreaterThanOrEqualTo((ushort)20);
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullOptions_ShouldThrow()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            null!,
            provider,
            serializer,
            logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullServiceProvider_ShouldThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            options,
            null!,
            serializer,
            logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullSerializer_ShouldThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            null!,
            logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullLogger_ShouldThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ChannelBatchProcessor_StartAndDispose_ShouldNotThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            logger);

        Func<Task> act = async () =>
        {
            processor.Start();
            processor.SetQueueMetadata("test-queue", "test-exchange", "tag-1");
            await Task.CompletedTask;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ChannelBatchProcessor_DoubleStart_ShouldNotThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            logger);

        Func<Task> act = async () =>
        {
            processor.Start();
            processor.Start();
            await Task.CompletedTask;
        };

        await act.Should().NotThrowAsync();
    }
}
