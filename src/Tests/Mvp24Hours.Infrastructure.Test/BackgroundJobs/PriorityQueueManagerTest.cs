//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Queues;
using Mvp24Hours.Infrastructure.Test.Support;

namespace Mvp24Hours.Infrastructure.Test.BackgroundJobs;

[Trait("Category", "Unit")]
public class PriorityQueueManagerTest
{
    [Fact]
    public void Enqueue_WithNullJob_ShouldThrowArgumentNullException()
    {
        var manager = new PriorityQueueManager();

        Action act = () => manager.Enqueue("default", JobPriority.Normal, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("job");
    }

    [Fact]
    public void Enqueue_WithNullOrWhitespaceQueueName_ShouldUseDefault()
    {
        var manager = new PriorityQueueManager();

        manager.Enqueue(null!, JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("a"));
        manager.Enqueue("   ", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("b"));

        manager.GetCount("default").Should().Be(2);
        manager.GetCount(null!).Should().Be(2);
    }

    [Fact]
    public void Dequeue_ShouldReturnHighestPriorityFirst()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("default", JobPriority.Low, BackgroundJobsTestHelpers.CreateQueuedJob("low"));
        manager.Enqueue("default", JobPriority.Critical, BackgroundJobsTestHelpers.CreateQueuedJob("critical"));
        manager.Enqueue("default", JobPriority.High, BackgroundJobsTestHelpers.CreateQueuedJob("high"));
        manager.Enqueue("default", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("normal"));

        manager.Dequeue("default")!.JobId.Should().Be("critical");
        manager.Dequeue("default")!.JobId.Should().Be("high");
        manager.Dequeue("default")!.JobId.Should().Be("normal");
        manager.Dequeue("default")!.JobId.Should().Be("low");
        manager.Dequeue("default").Should().BeNull();
    }

    [Fact]
    public void Dequeue_WhenQueueEmptyOrMissing_ShouldReturnNull()
    {
        var manager = new PriorityQueueManager();

        manager.Dequeue("missing").Should().BeNull();

        manager.Enqueue("q1", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob());
        _ = manager.Dequeue("q1");

        manager.Dequeue("q1").Should().BeNull();
    }

    [Fact]
    public void Dequeue_WithinSamePriority_ShouldBeFifo()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("default", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("first"));
        manager.Enqueue("default", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("second"));

        manager.Dequeue("default")!.JobId.Should().Be("first");
        manager.Dequeue("default")!.JobId.Should().Be("second");
    }

    [Fact]
    public void GetCount_ShouldRespectPriorityFilter()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("q1", JobPriority.High, BackgroundJobsTestHelpers.CreateQueuedJob("h1"));
        manager.Enqueue("q1", JobPriority.High, BackgroundJobsTestHelpers.CreateQueuedJob("h2"));
        manager.Enqueue("q1", JobPriority.Low, BackgroundJobsTestHelpers.CreateQueuedJob("l1"));

        manager.GetCount("q1", JobPriority.High).Should().Be(2);
        manager.GetCount("q1", JobPriority.Low).Should().Be(1);
        manager.GetCount("q1", JobPriority.Normal).Should().Be(0);
        manager.GetCount("q1").Should().Be(3);
        manager.GetCount("missing").Should().Be(0);
    }

    [Fact]
    public void GetTotalCount_ShouldSumAcrossQueues()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("q1", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("a"));
        manager.Enqueue("q2", JobPriority.High, BackgroundJobsTestHelpers.CreateQueuedJob("b"));
        manager.Enqueue("q2", JobPriority.Low, BackgroundJobsTestHelpers.CreateQueuedJob("c"));

        manager.GetTotalCount().Should().Be(3);
    }

    [Fact]
    public void Clear_ShouldRemoveOnlySpecifiedQueue()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("q1", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("a"));
        manager.Enqueue("q2", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("b"));

        manager.Clear("q1");

        manager.GetCount("q1").Should().Be(0);
        manager.GetCount("q2").Should().Be(1);
    }

    [Fact]
    public void ClearAll_ShouldRemoveAllQueues()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("q1", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("a"));
        manager.Enqueue("q2", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("b"));

        manager.ClearAll();

        manager.GetTotalCount().Should().Be(0);
        manager.GetQueueStatistics().Should().BeEmpty();
    }

    [Fact]
    public void GetQueueStatistics_ShouldReportPerPriorityCounts()
    {
        var manager = new PriorityQueueManager();
        manager.Enqueue("orders", JobPriority.Critical, BackgroundJobsTestHelpers.CreateQueuedJob("c"));
        manager.Enqueue("orders", JobPriority.High, BackgroundJobsTestHelpers.CreateQueuedJob("h1"));
        manager.Enqueue("orders", JobPriority.High, BackgroundJobsTestHelpers.CreateQueuedJob("h2"));
        manager.Enqueue("orders", JobPriority.Normal, BackgroundJobsTestHelpers.CreateQueuedJob("n"));
        manager.Enqueue("orders", JobPriority.Low, BackgroundJobsTestHelpers.CreateQueuedJob("l"));

        Dictionary<string, PriorityQueueManager.QueueStats> stats = manager.GetQueueStatistics();

        stats.Should().ContainKey("orders");
        PriorityQueueManager.QueueStats orders = stats["orders"];
        orders.CriticalCount.Should().Be(1);
        orders.HighCount.Should().Be(2);
        orders.NormalCount.Should().Be(1);
        orders.LowCount.Should().Be(1);
        orders.TotalCount.Should().Be(5);
    }
}
