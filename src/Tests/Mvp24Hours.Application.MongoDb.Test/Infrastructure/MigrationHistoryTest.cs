//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Infrastructure;

[Trait("Category", "Unit")]
public class MigrationHistoryTest
{
    #region [ MigrationStatus ]

    [Fact]
    public void MigrationStatus_Values_AreCorrect()
    {
        Assert.Equal(0, (int)MigrationStatus.Pending);
        Assert.Equal(1, (int)MigrationStatus.Running);
        Assert.Equal(2, (int)MigrationStatus.Completed);
        Assert.Equal(3, (int)MigrationStatus.Failed);
        Assert.Equal(4, (int)MigrationStatus.RolledBack);
    }

    [Fact]
    public void MigrationStatus_HasFiveValues()
    {
        var values = Enum.GetValues<MigrationStatus>();
        Assert.Equal(5, values.Length);
    }

    #endregion

    #region [ MongoDbMigrationHistory - Defaults ]

    [Fact]
    public void MongoDbMigrationHistory_DefaultValues_AreCorrect()
    {
        var history = new MongoDbMigrationHistory();

        Assert.Null(history.Id);
        Assert.Equal(0, history.Version);
        Assert.Equal(string.Empty, history.Description);
        Assert.Equal(string.Empty, history.TypeName);
        Assert.Equal(default, history.AppliedAt);
        Assert.Equal(0, history.DurationMs);
        Assert.Null(history.AppliedBy);
        Assert.Null(history.MachineName);
        Assert.Null(history.Error);
        Assert.Equal(MigrationStatus.Pending, history.Status);
    }

    [Fact]
    public void MongoDbMigrationHistory_CanAssignAllProperties()
    {
        var now = DateTime.UtcNow;
        var history = new MongoDbMigrationHistory
        {
            Id = "507f1f77bcf86cd799439011",
            Version = 3,
            Description = "Add user index",
            TypeName = "Migrations.AddUserIndex",
            AppliedAt = now,
            DurationMs = 1500,
            AppliedBy = "deploy-service",
            MachineName = "server01",
            Error = null,
            Status = MigrationStatus.Completed
        };

        Assert.Equal("507f1f77bcf86cd799439011", history.Id);
        Assert.Equal(3, history.Version);
        Assert.Equal("Add user index", history.Description);
        Assert.Equal("Migrations.AddUserIndex", history.TypeName);
        Assert.Equal(now, history.AppliedAt);
        Assert.Equal(1500, history.DurationMs);
        Assert.Equal("deploy-service", history.AppliedBy);
        Assert.Equal("server01", history.MachineName);
        Assert.Null(history.Error);
        Assert.Equal(MigrationStatus.Completed, history.Status);
    }

    [Fact]
    public void MongoDbMigrationHistory_CanSetErrorField()
    {
        var history = new MongoDbMigrationHistory
        {
            Status = MigrationStatus.Failed,
            Error = "Connection timeout"
        };

        Assert.Equal(MigrationStatus.Failed, history.Status);
        Assert.Equal("Connection timeout", history.Error);
    }

    [Fact]
    public void MongoDbMigrationHistory_IsSealed()
    {
        Assert.True(typeof(MongoDbMigrationHistory).IsSealed);
    }

    [Fact]
    public void MongoDbMigrationHistory_Status_AllStatesAreAssignable()
    {
        foreach (var status in Enum.GetValues<MigrationStatus>())
        {
            var history = new MongoDbMigrationHistory { Status = status };
            Assert.Equal(status, history.Status);
        }
    }

    [Fact]
    public void MongoDbMigrationHistory_DurationMs_CanBePositiveLong()
    {
        var history = new MongoDbMigrationHistory { DurationMs = long.MaxValue };
        Assert.Equal(long.MaxValue, history.DurationMs);
    }

    [Fact]
    public void MongoDbMigrationHistory_Version_CanBeNegativeOrZero()
    {
        var h0 = new MongoDbMigrationHistory { Version = 0 };
        var h1 = new MongoDbMigrationHistory { Version = 1 };
        var h100 = new MongoDbMigrationHistory { Version = 100 };

        Assert.Equal(0, h0.Version);
        Assert.Equal(1, h1.Version);
        Assert.Equal(100, h100.Version);
    }

    #endregion
}
