using Mvp24Hours.Infrastructure.Pipe.Channels;

namespace Mvp24Hours.Application.Pipe.Test.Channels;

[Trait("Category", "Unit")]
public class ChannelPipelineTest
{
    [Fact]
    public void AddStage_WithNullProcessor_ShouldThrow()
    {
        var pipeline = new ChannelPipeline<int, int>();

        Action act = () => pipeline.AddStage<int, int>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddStageAsync_WithNullProcessor_ShouldThrow()
    {
        var pipeline = new ChannelPipeline<int, int>();

        Action act = () => pipeline.AddStageAsync<int, int>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ProcessOneAsync_WithSingleStage_ShouldTransformInput()
    {
        await using ChannelPipeline<int, string> pipeline = new ChannelPipeline<int, string>()
            .AddStage<int, string>(n => $"value-{n}");

        string result = await pipeline.ProcessOneAsync(7);

        result.Should().Be("value-7");
    }

    [Fact]
    public async Task ProcessOneAsync_WithMultipleStages_ShouldChainTransformations()
    {
        await using ChannelPipeline<int, string> pipeline = new ChannelPipeline<int, string>()
            .AddStage<int, int>(n => n * 2)
            .AddStage<int, string>(n => $"result-{n}");

        string result = await pipeline.ProcessOneAsync(5);

        result.Should().Be("result-10");
    }

    [Fact]
    public async Task ProcessOneAsync_WithAsyncStage_ShouldAwaitProcessor()
    {
        await using ChannelPipeline<int, int> pipeline = new ChannelPipeline<int, int>()
            .AddStageAsync<int, int>(async (n, ct) =>
            {
                await Task.Delay(1, ct);
                return n + 1;
            });

        int result = await pipeline.ProcessOneAsync(3);

        result.Should().Be(4);
    }

    [Fact]
    public async Task ProcessAsync_WithEnumerable_ShouldYieldAllResults()
    {
        await using ChannelPipeline<int, int> pipeline = new ChannelPipeline<int, int>()
            .AddStage<int, int>(n => n + 1);

        List<int> results = [];
        await foreach (int item in pipeline.ProcessAsync([1, 2, 3]))
        {
            results.Add(item);
        }

        results.Should().Equal(2, 3, 4);
    }

    [Fact]
    public async Task ProcessAsync_WithAsyncEnumerable_ShouldYieldAllResults()
    {
        await using ChannelPipeline<int, int> pipeline = new ChannelPipeline<int, int>()
            .AddStage<int, int>(n => n * 10);

        static async IAsyncEnumerable<int> Inputs()
        {
            yield return 1;
            yield return 2;
        }

        List<int> results = [];
        await foreach (int item in pipeline.ProcessAsync(Inputs()))
        {
            results.Add(item);
        }

        results.Should().Equal(10, 20);
    }

    [Fact]
    public async Task ProcessParallelAsync_ShouldProcessAllInputs()
    {
        var options = new ChannelPipelineOptions { ChannelCapacity = 10 };
        await using ChannelPipeline<int, int> pipeline = new ChannelPipeline<int, int>(options)
            .AddStage<int, int>(n => n + 100);

        List<int> results = [];
        await foreach (int item in pipeline.ProcessParallelAsync(Enumerable.Range(1, 8), maxDegreeOfParallelism: 2))
        {
            results.Add(item);
        }

        results.Should().HaveCount(8);
        results.Should().OnlyContain(v => v >= 101 && v <= 108);
    }

    [Fact]
    public async Task ProcessOneAsync_WhenCancelled_ShouldThrow()
    {
        await using ChannelPipeline<int, int> pipeline = new ChannelPipeline<int, int>()
            .AddStageAsync<int, int>(async (_, ct) =>
            {
                await Task.Delay(500, ct);
                return 1;
            });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => pipeline.ProcessOneAsync(1, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DisposeAsync_ShouldPreventFurtherProcessing()
    {
        ChannelPipeline<int, int> pipeline = new ChannelPipeline<int, int>()
            .AddStage<int, int>(n => n);

        await pipeline.DisposeAsync();

        Func<Task> act = () => pipeline.ProcessOneAsync(1);

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void ChannelPipelineOptions_ShouldHaveExpectedDefaults()
    {
        var options = new ChannelPipelineOptions();

        options.ChannelCapacity.Should().Be(100);
        options.EnableTracing.Should().BeTrue();
        options.MaxDegreeOfParallelism.Should().Be(Environment.ProcessorCount);
    }

    [Fact]
    public void ChannelPipelineBuilder_StageAndThen_ShouldBuildPipeline()
    {
        ChannelPipeline<int, string> pipeline = ChannelPipeline
            .Create<int>()
            .Stage(n => n * 2)
            .Then(n => $"x{n}")
            .Build();

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void ChannelPipelineBuilder_StageAsyncAndThenAsync_ShouldBuildPipeline()
    {
        ChannelPipeline<int, string> pipeline = ChannelPipeline
            .Create<int>()
            .StageAsync(async (n, ct) =>
            {
                await Task.Delay(1, ct);
                return n + 1;
            })
            .ThenAsync(async (n, ct) =>
            {
                await Task.Delay(1, ct);
                return $"async-{n}";
            })
            .Build();

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void ChannelPipeline_Create_ShouldReturnBuilder()
    {
        ChannelPipelineBuilder<int> builder = ChannelPipeline.Create<int>(new ChannelPipelineOptions { ChannelCapacity = 50 });

        builder.Should().NotBeNull();
    }
}
