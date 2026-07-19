using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationResultTest
{
    [Fact]
    public void Succeeded_ShouldPopulateSuccessProperties()
    {
        var startedAt = DateTime.UtcNow.AddSeconds(-1);
        var duration = TimeSpan.FromMilliseconds(250);

        MigrationResult result = MigrationResult.Succeeded(["Migration_A"], duration, startedAt);

        result.Success.Should().BeTrue();
        result.AppliedMigrations.Should().ContainSingle("Migration_A");
        result.Duration.Should().Be(duration);
        result.StartedAt.Should().Be(startedAt);
        result.CompletedAt.Should().Be(startedAt.Add(duration));
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void RollbackSucceeded_ShouldPopulateRollbackProperties()
    {
        var startedAt = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(1);

        MigrationResult result = MigrationResult.RollbackSucceeded(["Migration_B"], duration, startedAt);

        result.Success.Should().BeTrue();
        result.RolledBackMigrations.Should().ContainSingle("Migration_B");
    }

    [Fact]
    public void Failed_ShouldPopulateFailureProperties()
    {
        var ex = new InvalidOperationException("boom");
        var startedAt = DateTime.UtcNow;

        MigrationResult result = MigrationResult.Failed("boom", ex, TimeSpan.Zero, startedAt);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("boom");
        result.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void NoMigrationsNeeded_ShouldReturnSuccessfulEmptyResult()
    {
        MigrationResult result = MigrationResult.NoMigrationsNeeded();

        result.Success.Should().BeTrue();
        result.AppliedMigrations.Should().BeEmpty();
        result.Duration.Should().Be(TimeSpan.Zero);
    }
}
