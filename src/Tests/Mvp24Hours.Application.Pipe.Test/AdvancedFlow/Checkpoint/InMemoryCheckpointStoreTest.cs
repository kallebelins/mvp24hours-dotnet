using Mvp24Hours.Infrastructure.Pipe.AdvancedFlow.Checkpoint;

namespace Mvp24Hours.Application.Pipe.Test.AdvancedFlow.Checkpoint;

[Trait("Category", "Unit")]
public class InMemoryCheckpointStoreTest
{
    private static PipelineCheckpoint CreateCheckpoint(
        string checkpointId,
        string executionId,
        CheckpointStatus status = CheckpointStatus.Created,
        DateTime? createdAt = null,
        string pipelineName = "test-pipeline")
    {
        return new PipelineCheckpoint
        {
            CheckpointId = checkpointId,
            PipelineExecutionId = executionId,
            PipelineName = pipelineName,
            StepIndex = 1,
            StepId = "step-1",
            Status = status,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    [Fact]
    public async Task SaveCheckpointAsync_Should_StoreCheckpoint()
    {
        var store = new InMemoryCheckpointStore();
        PipelineCheckpoint checkpoint = CreateCheckpoint("cp-1", "exec-1");

        await store.SaveCheckpointAsync(checkpoint);
        PipelineCheckpoint? loaded = await store.GetCheckpointAsync("cp-1");

        loaded.Should().NotBeNull();
        loaded!.PipelineExecutionId.Should().Be("exec-1");
        store.Count.Should().Be(1);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_Should_ReturnMostRecent()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1", createdAt: DateTime.UtcNow.AddMinutes(-5)));
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-2", "exec-1", createdAt: DateTime.UtcNow));

        PipelineCheckpoint? latest = await store.GetLatestCheckpointAsync("exec-1");

        latest!.CheckpointId.Should().Be("cp-2");
    }

    [Fact]
    public async Task GetCheckpointsAsync_Should_ReturnOrderedList()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-2", "exec-1", createdAt: DateTime.UtcNow));
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1", createdAt: DateTime.UtcNow.AddMinutes(-1)));

        IReadOnlyList<PipelineCheckpoint> checkpoints = await store.GetCheckpointsAsync("exec-1");

        checkpoints.Select(c => c.CheckpointId).Should().Equal("cp-1", "cp-2");
    }

    [Fact]
    public async Task UpdateCheckpointStatusAsync_Should_ChangeStatus()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1"));

        await store.UpdateCheckpointStatusAsync("cp-1", CheckpointStatus.Failed, "error");
        PipelineCheckpoint? updated = await store.GetCheckpointAsync("cp-1");

        updated!.Status.Should().Be(CheckpointStatus.Failed);
        updated.ErrorMessage.Should().Be("error");
    }

    [Fact]
    public async Task DeleteCheckpointAsync_Should_RemoveCheckpoint()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1"));

        await store.DeleteCheckpointAsync("cp-1");

        store.Count.Should().Be(0);
        (await store.GetCheckpointAsync("cp-1")).Should().BeNull();
    }

    [Fact]
    public async Task DeleteCheckpointsAsync_Should_RemoveAllForExecution()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1"));
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-2", "exec-1"));
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-3", "exec-2"));

        await store.DeleteCheckpointsAsync("exec-1");

        store.Count.Should().Be(1);
        (await store.GetCheckpointsAsync("exec-1")).Should().BeEmpty();
    }

    [Fact]
    public async Task GetResumableCheckpointsAsync_Should_FilterByStatusAndName()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1", CheckpointStatus.Paused, pipelineName: "orders"));
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-2", "exec-2", CheckpointStatus.Created, pipelineName: "orders"));
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-3", "exec-3", CheckpointStatus.Failed, pipelineName: "billing"));

        IReadOnlyList<PipelineCheckpoint> resumable = await store.GetResumableCheckpointsAsync("orders");

        resumable.Should().ContainSingle(c => c.CheckpointId == "cp-1");
    }

    [Fact]
    public async Task CleanupExpiredCheckpointsAsync_Should_RemoveOldEntries()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("old", "exec-1", createdAt: DateTime.UtcNow.AddHours(-2)));
        await store.SaveCheckpointAsync(CreateCheckpoint("new", "exec-2", createdAt: DateTime.UtcNow));

        int removed = await store.CleanupExpiredCheckpointsAsync(TimeSpan.FromHours(1));

        removed.Should().Be(1);
        store.Count.Should().Be(1);
    }

    [Fact]
    public async Task SaveCheckpointAsync_Should_ThrowWhenCheckpointIsNull()
    {
        var store = new InMemoryCheckpointStore();

        Func<Task> act = () => store.SaveCheckpointAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Clear_Should_RemoveAllCheckpoints()
    {
        var store = new InMemoryCheckpointStore();
        await store.SaveCheckpointAsync(CreateCheckpoint("cp-1", "exec-1"));

        store.Clear();

        store.Count.Should().Be(0);
    }
}
