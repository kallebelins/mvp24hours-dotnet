using Mvp24Hours.Infrastructure.CronJob.Dependencies;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Dependencies;

[Trait("Category", "Unit")]
public class CronJobDependencyTest
{
    [Fact]
    public void CronJobDependency_ShouldExposeConfiguredValues()
    {
        var dependency = new CronJobDependency(
            "ReportJob",
            ["DataJob", "CleanupJob"],
            requireSuccess: false,
            maxAge: TimeSpan.FromHours(2));

        dependency.DependentJobName.Should().Be("ReportJob");
        dependency.RequiredJobNames.Should().BeEquivalentTo(["DataJob", "CleanupJob"]);
        dependency.RequireSuccess.Should().BeFalse();
        dependency.MaxAge.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public void CronJobDependency_ShouldThrow_WhenDependentJobNameIsNull()
    {
        Action act = () => _ = new CronJobDependency(null!, ["A"]);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dependentJobName");
    }

    [Fact]
    public void CronJobDependencyBuilder_ShouldBuildDependency()
    {
        ICronJobDependency dependency = CronJobDependency.For("ReportJob")
            .DependsOn("DataJob")
            .DependsOn<CleanupJobMarker>()
            .WithSuccessRequired()
            .WithMaxAge(TimeSpan.FromMinutes(30))
            .Build();

        dependency.DependentJobName.Should().Be("ReportJob");
        dependency.RequiredJobNames.Should().BeEquivalentTo(["DataJob", nameof(CleanupJobMarker)]);
        dependency.RequireSuccess.Should().BeTrue();
        dependency.MaxAge.Should().Be(TimeSpan.FromMinutes(30));
    }

    private sealed class CleanupJobMarker;
}

[Trait("Category", "Unit")]
public class InMemoryCronJobDependencyTrackerTest
{
    private readonly InMemoryCronJobDependencyTracker _tracker = new();

    [Fact]
    public async Task AreDependenciesSatisfiedAsync_ShouldReturnTrue_WhenNoDependencies()
    {
        (await _tracker.AreDependenciesSatisfiedAsync("IndependentJob")).Should().BeTrue();
    }

    [Fact]
    public async Task AreDependenciesSatisfiedAsync_ShouldReturnFalse_WhenRequiredJobNotCompleted()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"]));

        (await _tracker.AreDependenciesSatisfiedAsync("ReportJob")).Should().BeFalse();
        IReadOnlyList<string> unsatisfied = await _tracker.GetUnsatisfiedDependenciesAsync("ReportJob");
        unsatisfied.Should().ContainSingle().Which.Should().Be("DataJob");
    }

    [Fact]
    public async Task AreDependenciesSatisfiedAsync_ShouldReturnTrue_AfterSuccessfulCompletion()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"], requireSuccess: true));
        await _tracker.RecordCompletionAsync("DataJob", success: true, Guid.NewGuid());

        (await _tracker.AreDependenciesSatisfiedAsync("ReportJob")).Should().BeTrue();
    }

    [Fact]
    public async Task AreDependenciesSatisfiedAsync_ShouldReturnFalse_WhenSuccessRequiredButJobFailed()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"], requireSuccess: true));
        await _tracker.RecordCompletionAsync("DataJob", success: false, Guid.NewGuid());

        (await _tracker.AreDependenciesSatisfiedAsync("ReportJob")).Should().BeFalse();
    }

    [Fact]
    public async Task AreDependenciesSatisfiedAsync_ShouldReturnTrue_WhenSuccessNotRequiredAndJobFailed()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"], requireSuccess: false));
        await _tracker.RecordCompletionAsync("DataJob", success: false, Guid.NewGuid());

        (await _tracker.AreDependenciesSatisfiedAsync("ReportJob")).Should().BeTrue();
    }

    [Fact]
    public async Task AreDependenciesSatisfiedAsync_ShouldReturnFalse_WhenCompletionTooOld()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"], maxAge: TimeSpan.FromMilliseconds(1)));
        await _tracker.RecordCompletionAsync("DataJob", success: true, Guid.NewGuid());
        await Task.Delay(10);

        (await _tracker.AreDependenciesSatisfiedAsync("ReportJob")).Should().BeFalse();
    }

    [Fact]
    public void GetDependentJobs_ShouldReturnReverseMapping()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"]));
        _tracker.RegisterDependency(new CronJobDependency("EmailJob", ["DataJob"]));

        _tracker.GetDependentJobs("DataJob").Should().BeEquivalentTo(["ReportJob", "EmailJob"]);
    }

    [Fact]
    public void GetDependencies_ShouldReturnRegisteredDependencies()
    {
        ICronJobDependency dependency = new CronJobDependency("ReportJob", ["DataJob"]);
        _tracker.RegisterDependency(dependency);

        _tracker.GetDependencies("ReportJob").Should().ContainSingle().Which.Should().BeSameAs(dependency);
        _tracker.GetDependencies("Other").Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCompletions_ShouldResetSatisfaction()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"]));
        await _tracker.RecordCompletionAsync("DataJob", success: true, Guid.NewGuid());

        _tracker.ClearCompletions();

        (await _tracker.AreDependenciesSatisfiedAsync("ReportJob")).Should().BeFalse();
    }

    [Fact]
    public void Clear_ShouldRemoveDependenciesAndCompletions()
    {
        _tracker.RegisterDependency(new CronJobDependency("ReportJob", ["DataJob"]));
        _tracker.Clear();

        _tracker.GetDependencies("ReportJob").Should().BeEmpty();
        _tracker.GetDependentJobs("DataJob").Should().BeEmpty();
    }
}
