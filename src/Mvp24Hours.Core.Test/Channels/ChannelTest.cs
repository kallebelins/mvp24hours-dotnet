//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Infrastructure.Channels;
using Mvp24Hours.Core.Infrastructure.Channels;
using System.Threading.Channels;
using Xunit;

namespace Mvp24Hours.Core.Test.Channels;

/// <summary>
/// Unit tests for System.Threading.Channels integration.
/// </summary>
public class ChannelTest
{
    #region MvpChannel Tests

    [Fact]
    public void CreateBoundedChannel_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        using var channel = MvpChannel<int>.CreateBounded(100);

        // Assert
        Assert.True(channel.Options.IsBounded);
        Assert.Equal(100, channel.Options.Capacity);
    }

    [Fact]
    public void CreateUnboundedChannel_ShouldHaveCorrectOptions()
    {
        // Arrange & Act
        using var channel = MvpChannel<int>.CreateUnbounded();

        // Assert
        Assert.False(channel.Options.IsBounded);
    }

    [Fact]
    public async Task WriteAsync_ShouldWriteItem()
    {
        // Arrange
        using var channel = new MvpChannel<string>();
        var item = "test";

        // Act
        await channel.Writer.WriteAsync(item);

        // Assert
        Assert.Equal(1, channel.Count);
    }

    [Fact]
    public async Task ReadAsync_ShouldReadItem()
    {
        // Arrange
        using var channel = new MvpChannel<string>();
        var expected = "test";
        await channel.Writer.WriteAsync(expected);

        // Act
        var actual = await channel.Reader.ReadAsync();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryWrite_ShouldReturnTrueWhenSpaceAvailable()
    {
        // Arrange
        using var channel = new MvpChannel<int>(MvpChannelOptions.Bounded(10));

        // Act
        var result = channel.Writer.TryWrite(42);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void TryRead_ShouldReturnFalseWhenEmpty()
    {
        // Arrange
        using var channel = new MvpChannel<int>();

        // Act
        var result = channel.Reader.TryRead(out var item);

        // Assert
        Assert.False(result);
        Assert.Equal(default, item);
    }

    [Fact]
    public async Task TryComplete_ShouldCompleteChannel()
    {
        // Arrange
        using var channel = new MvpChannel<int>();

        // Act
        var result = channel.Writer.TryComplete();

        // Assert
        Assert.True(result);
        Assert.True(channel.IsCompleted);
        await Assert.ThrowsAsync<ChannelClosedException>(async () =>
            await channel.Writer.WriteAsync(1));
    }

    [Fact]
    public async Task WriteManyAsync_ShouldWriteAllItems()
    {
        // Arrange
        using var channel = new MvpChannel<int>();
        var items = new[] { 1, 2, 3, 4, 5 };

        // Act
        await channel.Writer.WriteManyAsync(items);

        // Assert
        Assert.Equal(5, channel.Count);
    }

    [Fact]
    public async Task ReadAllAsync_ShouldReadAllItems()
    {
        // Arrange
        using var channel = new MvpChannel<int>();
        var items = new[] { 1, 2, 3 };
        await channel.Writer.WriteManyAsync(items);
        channel.Writer.TryComplete();

        // Act
        var result = new List<int>();
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(items, result);
    }

    [Fact]
    public async Task ReadBatchAsync_ShouldReturnBatches()
    {
        // Arrange
        using var channel = new MvpChannel<int>();
        for (int i = 1; i <= 10; i++)
        {
            await channel.Writer.WriteAsync(i);
        }
        channel.Writer.TryComplete();

        // Act
        var batches = new List<IReadOnlyList<int>>();
        await foreach (var batch in channel.Reader.ReadBatchAsync(3, TimeSpan.FromSeconds(1)))
        {
            batches.Add(batch);
        }

        // Assert
        Assert.True(batches.Count >= 3); // At least 3 batches for 10 items with batch size 3
        Assert.Equal(10, batches.Sum(b => b.Count)); // Total items should be 10
    }

    #endregion

    #region ChannelFactory Tests

    [Fact]
    public void ChannelFactory_Create_ShouldCreateChannel()
    {
        // Arrange
        var factory = new ChannelFactory();

        // Act
        var channel = factory.Create<string>();

        // Assert
        Assert.NotNull(channel);
        Assert.True(channel.Options.IsBounded);
    }

    [Fact]
    public void ChannelFactory_CreateUnbounded_ShouldCreateUnboundedChannel()
    {
        // Arrange
        var factory = new ChannelFactory();

        // Act
        var channel = factory.CreateUnbounded<string>();

        // Assert
        Assert.NotNull(channel);
        Assert.False(channel.Options.IsBounded);
    }

    [Fact]
    public void ChannelFactory_CreateBounded_ShouldCreateBoundedChannel()
    {
        // Arrange
        var factory = new ChannelFactory();

        // Act
        var channel = factory.CreateBounded<string>(50);

        // Assert
        Assert.NotNull(channel);
        Assert.True(channel.Options.IsBounded);
        Assert.Equal(50, channel.Options.Capacity);
    }

    [Fact]
    public void ChannelFactory_CreateBounded_ShouldThrowForZeroCapacity()
    {
        // Arrange
        var factory = new ChannelFactory();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateBounded<string>(0));
    }

    #endregion

    #region Static Channels Helper Tests

    [Fact]
    public void Channels_CreateHighThroughput_ShouldConfigureCorrectly()
    {
        // Arrange & Act
        using var channel = Channels.CreateHighThroughput<int>(500);

        // Assert
        Assert.True(channel.Options.IsBounded);
        Assert.Equal(500, channel.Options.Capacity);
        Assert.True(channel.Options.AllowSynchronousContinuations);
        Assert.True(channel.Options.SingleReader);
    }

    [Fact]
    public void Channels_CreateDropOldest_ShouldConfigureCorrectly()
    {
        // Arrange & Act
        using var channel = Channels.CreateDropOldest<int>(50);

        // Assert
        Assert.True(channel.Options.IsBounded);
        Assert.Equal(50, channel.Options.Capacity);
        Assert.Equal(BoundedChannelFullMode.DropOldest, channel.Options.FullMode);
    }

    [Fact]
    public void Channels_CreateDropNewest_ShouldConfigureCorrectly()
    {
        // Arrange & Act
        using var channel = Channels.CreateDropNewest<int>(50);

        // Assert
        Assert.True(channel.Options.IsBounded);
        Assert.Equal(BoundedChannelFullMode.DropNewest, channel.Options.FullMode);
    }

    #endregion

    #region MvpChannelOptions Tests

    [Fact]
    public void MvpChannelOptions_Unbounded_ShouldReturnUnboundedOptions()
    {
        // Act
        var options = MvpChannelOptions.Unbounded();

        // Assert
        Assert.False(options.IsBounded);
    }

    [Fact]
    public void MvpChannelOptions_Bounded_ShouldReturnBoundedOptions()
    {
        // Act
        var options = MvpChannelOptions.Bounded(200, BoundedChannelFullMode.DropOldest);

        // Assert
        Assert.True(options.IsBounded);
        Assert.Equal(200, options.Capacity);
        Assert.Equal(BoundedChannelFullMode.DropOldest, options.FullMode);
    }

    #endregion

    #region Backpressure Tests

    [Fact]
    public async Task BoundedChannel_ShouldBlockWhenFull()
    {
        // Arrange
        using var channel = new MvpChannel<int>(MvpChannelOptions.Bounded(2, BoundedChannelFullMode.Wait));

        // Act
        await channel.Writer.WriteAsync(1);
        await channel.Writer.WriteAsync(2);

        // Channel is now full - next write should block
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var writeTask = channel.Writer.WriteAsync(3, cts.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await writeTask);
    }

    [Fact]
    public async Task BoundedChannel_DropOldest_ShouldNotBlock()
    {
        // Arrange
        using var channel = new MvpChannel<int>(MvpChannelOptions.DropOldest(2));

        // Act - Write 3 items to a channel with capacity 2
        channel.Writer.TryWrite(1);
        channel.Writer.TryWrite(2);
        channel.Writer.TryWrite(3); // This should succeed, dropping oldest

        // Assert
        var items = new List<int>();
        while (channel.Reader.TryRead(out var item))
        {
            items.Add(item);
        }

        Assert.Equal(2, items.Count);
        Assert.DoesNotContain(1, items); // Oldest item should be dropped
    }

    #endregion

    #region ChannelMessage Tests

    [Fact]
    public void ChannelMessage_Create_ShouldSetProperties()
    {
        // Arrange
        var payload = new { Name = "Test" };

        // Act
        var message = ChannelMessage<object>.Create(payload);

        // Assert
        Assert.NotEqual(Guid.Empty, message.MessageId);
        Assert.Equal(payload, message.Payload);
        Assert.True(message.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ChannelMessage_CreateWithTracking_ShouldSetCorrelationId()
    {
        // Arrange
        var payload = "test";
        var correlationId = Guid.NewGuid().ToString();
        var causationId = Guid.NewGuid().ToString();

        // Act
        var message = ChannelMessage<string>.CreateWithTracking(payload, correlationId, causationId);

        // Assert
        Assert.Equal(correlationId, message.CorrelationId);
        Assert.Equal(causationId, message.CausationId);
    }

    #endregion

    #region ProducerConsumer Tests

    [Fact]
    public async Task ProducerConsumer_ShouldProcessAllItems()
    {
        // Arrange
        var processed = new List<int>();
        var semaphore = new SemaphoreSlim(1);

        await using var pc = new ProducerConsumer<int>(
            async (item, ct) =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    processed.Add(item);
                }
                finally
                {
                    semaphore.Release();
                }
            },
            workerCount: 2);

        // Act
        await pc.RunAsync(async (producer, ct) =>
        {
            for (int i = 0; i < 10; i++)
            {
                await producer.ProduceAsync(i, ct);
            }
        });

        // Assert
        Assert.Equal(10, processed.Count);
    }

    [Fact]
    public async Task ProducerConsumer_WithResults_ShouldReturnResults()
    {
        // Arrange
        await using var pc = new ProducerConsumer<int, string>(
            async (item, ct) =>
            {
                await Task.Delay(1, ct);
                return $"Processed: {item}";
            },
            workerCount: 2);

        // Act
        pc.Start();
        await pc.ProduceAsync(1);
        await pc.ProduceAsync(2);
        await pc.ProduceAsync(3);
        pc.Complete();

        var results = new List<string>();
        await foreach (var result in pc.GetResultsAsync())
        {
            results.Add(result);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.StartsWith("Processed:", r));
    }

    [Fact]
    public async Task ProducerConsumer_Cancellation_ShouldStopProcessing()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var processedCount = 0;

        await using var pc = new ProducerConsumer<int>(
            async (item, ct) =>
            {
                await Task.Delay(100, ct);
                Interlocked.Increment(ref processedCount);
            },
            workerCount: 1);

        // Act
        pc.Start();
        await pc.ProduceAsync(1);
        await pc.ProduceAsync(2);
        await pc.ProduceAsync(3);

        // Cancel after short delay
        cts.CancelAfter(50);

        pc.Complete();

        try
        {
            await pc.WaitForCompletionAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        Assert.True(processedCount < 3);
    }

    #endregion
}

