using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Infrastructure.Channels;

namespace Mvp24Hours.Core.Test.Infrastructure;

[Trait("Category", "Unit")]
public class ChannelsTest
{
    [Fact]
    public async Task MvpChannel_WriteAndRead_RoundTripsItem()
    {
        using var channel = new MvpChannel<string>(MvpChannelOptions.Bounded(10));

        await channel.Writer.WriteAsync("hello");
        string result = await channel.Reader.ReadAsync();

        result.Should().Be("hello");
    }

    [Fact]
    public async Task MvpChannel_ReadBatchAsync_ReturnsBatch()
    {
        using var channel = new MvpChannel<int>(MvpChannelOptions.Unbounded());

        await channel.Writer.WriteManyAsync([1, 2, 3]);
        channel.Writer.TryComplete();

        IReadOnlyList<int> batch = await channel.Reader.ReadBatchAsync(2, TimeSpan.FromSeconds(1)).FirstAsync();

        batch.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void ChannelFactory_CreateBounded_WithInvalidCapacity_Throws()
    {
        var factory = new ChannelFactory(NullLogger<ChannelFactory>.Instance);

        Action act = () => factory.CreateBounded<string>(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Channels_StaticHelpers_CreateConfiguredChannels()
    {
        IChannel<string> unbounded = Channels.CreateUnbounded<string>();
        IChannel<string> bounded = Channels.CreateBounded<string>(5);
        IChannel<string> highThroughput = Channels.CreateHighThroughput<string>(100);
        IChannel<string> dropOldest = Channels.CreateDropOldest<string>(10);

        unbounded.Options.IsBounded.Should().BeFalse();
        bounded.Options.Capacity.Should().Be(5);
        highThroughput.Options.AllowSynchronousContinuations.Should().BeTrue();
        dropOldest.Options.FullMode.Should().Be(System.Threading.Channels.BoundedChannelFullMode.DropOldest);
    }

    [Fact]
    public async Task ProducerConsumer_ProcessesItems()
    {
        var processed = new List<int>();
        await using var pc = new ProducerConsumer<int>(
            async (item, _) =>
            {
                processed.Add(item);
                await Task.CompletedTask;
            },
            workerCount: 1,
            options: new ProducerConsumerOptions { Capacity = 10 });

        pc.Start();
        await pc.ProduceAsync(1);
        await pc.ProduceAsync(2);
        pc.Complete();
        await pc.WaitForCompletionAsync();

        processed.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task ProducerConsumer_WithResults_ReturnsMappedValues()
    {
        await using var pc = new ProducerConsumer<int, string>(
            async (item, _) =>
            {
                await Task.CompletedTask;
                return item.ToString();
            },
            workerCount: 1);

        pc.Start();
        await pc.ProduceAsync(7);
        pc.Complete();

        string result = await pc.GetResultsAsync().FirstAsync();

        result.Should().Be("7");
    }

    [Fact]
    public async Task ProducerConsumer_ProduceAfterComplete_Throws()
    {
        await using var pc = new ProducerConsumer<int>((_, _) => Task.CompletedTask, workerCount: 1);
        pc.Complete();

        Func<Task> act = async () => await pc.ProduceAsync(1);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
