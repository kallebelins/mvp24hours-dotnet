//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for streaming requests (IAsyncEnumerable).
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class StreamTest
{
    private readonly IMediator _mediator;
    private readonly IStreamSender _streamSender;

    public StreamTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(GetItemsStreamRequest).Assembly);
        var sp = services.BuildServiceProvider();
        _mediator = sp.GetRequiredService<IMediator>();
        _streamSender = sp.GetRequiredService<IStreamSender>();
    }

    [Fact, Priority(1)]
    public async Task CreateStream_ShouldReturnAllItems()
    {
        // Arrange
        var request = new GetItemsStreamRequest { Count = 5 };
        var results = new List<int>();

        // Act
        await foreach (var item in _mediator.CreateStream(request))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(5, results.Count);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, results);
    }

    [Fact, Priority(2)]
    public async Task CreateStream_WithZeroCount_ShouldReturnEmpty()
    {
        // Arrange
        var request = new GetItemsStreamRequest { Count = 0 };
        var results = new List<int>();

        // Act
        await foreach (var item in _mediator.CreateStream(request))
        {
            results.Add(item);
        }

        // Assert
        Assert.Empty(results);
    }

    [Fact, Priority(3)]
    public async Task CreateStream_ShouldRespectCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var request = new GetItemsStreamRequest { Count = 100, DelayMs = 50 };
        var results = new List<int>();

        // Act
        await foreach (var item in _mediator.CreateStream(request, cts.Token))
        {
            results.Add(item);
            if (results.Count >= 3)
            {
                cts.Cancel();
            }
        }

        // Assert
        Assert.True(results.Count <= 4); // Might get one more before cancellation takes effect
    }

    [Fact, Priority(4)]
    public async Task CreateStream_WithStringItems_ShouldWork()
    {
        // Arrange
        var request = new GetNamesStreamRequest
        {
            Names = new List<string> { "Alice", "Bob", "Charlie" }
        };
        var results = new List<string>();

        // Act
        await foreach (var name in _mediator.CreateStream(request))
        {
            results.Add(name);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains("Alice", results);
        Assert.Contains("Bob", results);
        Assert.Contains("Charlie", results);
    }

    [Fact, Priority(5)]
    public async Task CreateStream_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var _ in _mediator.CreateStream<int>(null!))
            {
                // Should not reach here
            }
        });
    }

    [Fact, Priority(6)]
    public async Task IStreamSender_ShouldWorkIndependently()
    {
        // Arrange
        var request = new GetItemsStreamRequest { Count = 3 };
        var results = new List<int>();

        // Act
        await foreach (var item in _streamSender.CreateStream(request))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact, Priority(7)]
    public async Task CreateStream_WithDelays_ShouldProcessIncrementally()
    {
        // Arrange
        var request = new GetItemsStreamRequest { Count = 3, DelayMs = 10 };
        var timestamps = new List<DateTime>();

        // Act
        await foreach (var _ in _mediator.CreateStream(request))
        {
            timestamps.Add(DateTime.UtcNow);
        }

        // Assert
        Assert.Equal(3, timestamps.Count);
        // Verify there were delays between items
        for (int i = 1; i < timestamps.Count; i++)
        {
            var diff = timestamps[i] - timestamps[i - 1];
            Assert.True(diff.TotalMilliseconds >= 5, "Items should be received with delays");
        }
    }

    [Fact, Priority(8)]
    public async Task CreateStream_WithEmptyNames_ShouldReturnEmpty()
    {
        // Arrange
        var request = new GetNamesStreamRequest { Names = new List<string>() };
        var count = 0;

        // Act
        await foreach (var _ in _mediator.CreateStream(request))
        {
            count++;
        }

        // Assert
        Assert.Equal(0, count);
    }
}

