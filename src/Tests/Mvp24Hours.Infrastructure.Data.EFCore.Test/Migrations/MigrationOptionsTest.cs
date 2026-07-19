using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationOptionsTest
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var options = new MigrationOptions();

        options.AutoMigrateOnStartup.Should().BeFalse();
        options.ThrowOnPendingMigrations.Should().BeFalse();
        options.LogPendingMigrations.Should().BeTrue();
        options.MigrationTimeout.Should().Be(TimeSpan.FromMinutes(5));
        options.UseTransactions.Should().BeTrue();
        options.EnsureDatabaseCreated.Should().BeTrue();
        options.EnableDataSeeding.Should().BeFalse();
        options.SeedOnlyOnMigration.Should().BeTrue();
        options.SeedInTransaction.Should().BeTrue();
        options.MaxRetryAttempts.Should().Be(3);
        options.RetryDelay.Should().Be(TimeSpan.FromSeconds(5));
        options.UseExponentialBackoff.Should().BeTrue();
        options.UseDistributedLock.Should().BeTrue();
        options.LockName.Should().Be("ef-core-migration-lock");
        options.EnableDetailedLogging.Should().BeTrue();
        options.LogMigrationSql.Should().BeFalse();
        options.EnableTelemetry.Should().BeTrue();
    }

    [Fact]
    public void Development_ShouldEnableAutoMigrateAndSeeding()
    {
        MigrationOptions options = MigrationOptions.Development();

        options.AutoMigrateOnStartup.Should().BeTrue();
        options.EnableDataSeeding.Should().BeTrue();
        options.UseDistributedLock.Should().BeFalse();
        options.MaxRetryAttempts.Should().Be(1);
    }

    [Fact]
    public void Production_ShouldDisableAutoMigrateAndEnableThrowOnPending()
    {
        MigrationOptions options = MigrationOptions.Production();

        options.AutoMigrateOnStartup.Should().BeFalse();
        options.ThrowOnPendingMigrations.Should().BeTrue();
        options.EnableDataSeeding.Should().BeFalse();
        options.CreateSchemaSnapshot.Should().BeTrue();
    }
}
