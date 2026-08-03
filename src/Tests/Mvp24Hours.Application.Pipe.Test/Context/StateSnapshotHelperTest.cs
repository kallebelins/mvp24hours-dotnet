using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;

namespace Mvp24Hours.Application.Pipe.Test.Context;

[Trait("Category", "Unit")]
public class StateSnapshotHelperTest
{
    [Fact]
    public void CaptureMessageState_Should_CaptureBasicState()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage("key", "value");
        message.Messages.Add(new MessageResult("step", "ok", MessageType.Info));

        object state = StateSnapshotHelper.CaptureMessageState(message, "Op1");

        state.Should().NotBeNull();
    }

    [Fact]
    public void CaptureMessageState_Should_IncludeContents_WhenRequested()
    {
        IPipelineMessage message = PipeTestHelpers.CreateMessage("name", "alpha");

        object state = StateSnapshotHelper.CaptureMessageState(message, "Op1", includeContents: true);

        state.Should().NotBeNull();
    }

    [Fact]
    public void CaptureMessageState_Should_Throw_WhenMessageIsNull()
    {
        Action act = () => StateSnapshotHelper.CaptureMessageState(null!, "Op1");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CompareSnapshots_Should_ReturnDurationAndMetadataChanges()
    {
        var before = new PipelineStateSnapshot
        {
            OperationName = "Before",
            CorrelationId = "corr-1",
            CapturedAt = DateTime.UtcNow.AddSeconds(-1),
            SequenceNumber = 1,
            Metadata = new Dictionary<string, object> { ["a"] = 1 }
        };
        var after = new PipelineStateSnapshot
        {
            OperationName = "After",
            CorrelationId = "corr-1",
            CapturedAt = DateTime.UtcNow,
            SequenceNumber = 2,
            Metadata = new Dictionary<string, object> { ["a"] = 2, ["b"] = 3 }
        };

        object comparison = StateSnapshotHelper.CompareSnapshots(before, after);

        comparison.Should().NotBeNull();
    }

    [Fact]
    public void CompareSnapshots_Should_Throw_WhenSnapshotIsNull()
    {
        var snapshot = new PipelineStateSnapshot
        {
            OperationName = "Op",
            CorrelationId = "corr"
        };

        Action act = () => StateSnapshotHelper.CompareSnapshots(snapshot, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SerializeSnapshot_Should_ReturnJson()
    {
        var snapshot = new PipelineStateSnapshot
        {
            OperationName = "Serialize",
            CorrelationId = "corr-json",
            Description = "test",
            SequenceNumber = 1
        };

        string json = StateSnapshotHelper.SerializeSnapshot(snapshot);

        json.Should().Contain("Serialize");
        json.Should().Contain("corr-json");
    }

    [Fact]
    public void SerializeSnapshot_Should_Throw_WhenSnapshotIsNull()
    {
        Action act = () => StateSnapshotHelper.SerializeSnapshot(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateTimeline_Should_ReturnEmptySummary_WhenNoSnapshots()
    {
        object timeline = StateSnapshotHelper.CreateTimeline([]);

        timeline.Should().NotBeNull();
    }

    [Fact]
    public void CreateTimeline_Should_BuildOperationTimeline()
    {
        var snapshots = new List<PipelineStateSnapshot>
        {
            new()
            {
                OperationName = "Start",
                CorrelationId = "corr-timeline",
                CapturedAt = DateTime.UtcNow.AddSeconds(-2),
                SequenceNumber = 1
            },
            new()
            {
                OperationName = "End",
                CorrelationId = "corr-timeline",
                CapturedAt = DateTime.UtcNow,
                SequenceNumber = 2,
                Description = "Error: failed"
            }
        };

        object timeline = StateSnapshotHelper.CreateTimeline(snapshots);

        timeline.Should().NotBeNull();
    }

    [Fact]
    public void FilterByOperation_Should_MatchWildcardPattern()
    {
        var snapshots = new List<PipelineStateSnapshot>
        {
            new() { OperationName = "ValidateInput", CorrelationId = "c1" },
            new() { OperationName = "ValidateOutput", CorrelationId = "c1" },
            new() { OperationName = "Persist", CorrelationId = "c1" }
        };

        IEnumerable<PipelineStateSnapshot> filtered =
            StateSnapshotHelper.FilterByOperation(snapshots, "Validate*");

        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void FilterByOperation_Should_ReturnAll_WhenPatternIsEmpty()
    {
        var snapshots = new List<PipelineStateSnapshot>
        {
            new() { OperationName = "A", CorrelationId = "c1" },
            new() { OperationName = "B", CorrelationId = "c1" }
        };

        IEnumerable<PipelineStateSnapshot> filtered =
            StateSnapshotHelper.FilterByOperation(snapshots, string.Empty);

        filtered.Should().HaveCount(2);
    }

    [Fact]
    public void GetErrorSnapshots_Should_ReturnErrorDescriptions()
    {
        var snapshots = new List<PipelineStateSnapshot>
        {
            new() { OperationName = "Ok", CorrelationId = "c1" },
            new() { OperationName = "Process.Error", CorrelationId = "c1" },
            new() { OperationName = "Save", CorrelationId = "c1", Description = "Error: timeout" }
        };

        IEnumerable<PipelineStateSnapshot> errors = StateSnapshotHelper.GetErrorSnapshots(snapshots);

        errors.Should().HaveCount(2);
    }
}
