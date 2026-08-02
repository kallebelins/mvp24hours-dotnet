//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.ConnectionPool;
using Xunit;

namespace Mvp24Hours.Application.MongoDb.Test.Configuration;

[Trait("Category", "Unit")]
public class ConfigurationTest
{
    #region [ MongoDbOptions ]

    [Fact]
    public void MongoDbOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbOptions();

        Assert.Equal(string.Empty, opts.DatabaseName);
        Assert.Equal(string.Empty, opts.ConnectionString);
        Assert.False(opts.EnableTls);
        Assert.False(opts.EnableTransaction);
        Assert.Null(opts.Authentication);
        Assert.False(opts.EnableMultiTenancy);
        Assert.True(opts.TenantValidateOnUpdate);
        Assert.True(opts.TenantValidateOnDelete);
        Assert.True(opts.TenantThrowOnMissing);
        Assert.Null(opts.EncryptionKey);
        Assert.Null(opts.ReadPreference);
        Assert.Null(opts.WriteConcern);
        Assert.Null(opts.ReadConcern);
        Assert.Null(opts.ConnectionTimeoutSeconds);
        Assert.Null(opts.SocketTimeoutSeconds);
        Assert.Null(opts.MaxConnectionPoolSize);
        Assert.Null(opts.MinConnectionPoolSize);
        Assert.False(opts.EnableCommandLogging);
        Assert.True(opts.RetryReads);
        Assert.True(opts.RetryWrites);
    }

    [Fact]
    public void MongoDbOptions_CanAssignAllProperties()
    {
        var opts = new MongoDbOptions
        {
            DatabaseName = "testdb",
            ConnectionString = "mongodb://localhost:27017",
            EnableTls = true,
            EnableTransaction = true,
            EnableMultiTenancy = true,
            TenantValidateOnUpdate = false,
            TenantValidateOnDelete = false,
            TenantThrowOnMissing = false,
            EncryptionKey = "base64key",
            ReadPreference = "secondary",
            WriteConcern = "majority",
            ReadConcern = "majority",
            ConnectionTimeoutSeconds = 30,
            SocketTimeoutSeconds = 60,
            MaxConnectionPoolSize = 200,
            MinConnectionPoolSize = 5,
            EnableCommandLogging = true,
            RetryReads = false,
            RetryWrites = false
        };

        Assert.Equal("testdb", opts.DatabaseName);
        Assert.Equal("mongodb://localhost:27017", opts.ConnectionString);
        Assert.True(opts.EnableTls);
        Assert.True(opts.EnableTransaction);
        Assert.True(opts.EnableMultiTenancy);
        Assert.False(opts.TenantValidateOnUpdate);
        Assert.False(opts.TenantValidateOnDelete);
        Assert.False(opts.TenantThrowOnMissing);
        Assert.Equal("base64key", opts.EncryptionKey);
        Assert.Equal("secondary", opts.ReadPreference);
        Assert.Equal("majority", opts.WriteConcern);
        Assert.Equal("majority", opts.ReadConcern);
        Assert.Equal(30, opts.ConnectionTimeoutSeconds);
        Assert.Equal(60, opts.SocketTimeoutSeconds);
        Assert.Equal(200, opts.MaxConnectionPoolSize);
        Assert.Equal(5, opts.MinConnectionPoolSize);
        Assert.True(opts.EnableCommandLogging);
        Assert.False(opts.RetryReads);
        Assert.False(opts.RetryWrites);
    }

    [Fact]
    public void MongoDbOptions_IsSealed()
    {
        Assert.True(typeof(MongoDbOptions).IsSealed);
    }

    #endregion

    #region [ MongoDbBulkOperationOptions ]

    [Fact]
    public void MongoDbBulkOperationOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbBulkOperationOptions();

        Assert.Equal(1000, opts.BatchSize);
        Assert.True(opts.UseTransaction);
        Assert.Null(opts.ProgressCallback);
        Assert.Equal(300, opts.TimeoutSeconds);
        Assert.True(opts.IsOrdered);
        Assert.False(opts.BypassDocumentValidation);
        Assert.Equal(string.Empty, opts.WriteConcern);
        Assert.Equal(3, opts.MaxRetryAttempts);
        Assert.Equal(100, opts.RetryDelayMilliseconds);
    }

    [Fact]
    public void MongoDbBulkOperationOptions_Default_ReturnsNewInstance()
    {
        MongoDbBulkOperationOptions opts1 = MongoDbBulkOperationOptions.Default;
        MongoDbBulkOperationOptions opts2 = MongoDbBulkOperationOptions.Default;

        Assert.NotNull(opts1);
        Assert.NotSame(opts1, opts2);
        Assert.Equal(1000, opts1.BatchSize);
    }

    [Fact]
    public void MongoDbBulkOperationOptions_HighThroughput_HasCorrectValues()
    {
        MongoDbBulkOperationOptions opts = MongoDbBulkOperationOptions.HighThroughput;

        Assert.False(opts.IsOrdered);
        Assert.True(opts.BypassDocumentValidation);
        Assert.Equal("w1", opts.WriteConcern);
        Assert.Equal(5000, opts.BatchSize);
    }

    [Fact]
    public void MongoDbBulkOperationOptions_HighIntegrity_HasCorrectValues()
    {
        MongoDbBulkOperationOptions opts = MongoDbBulkOperationOptions.HighIntegrity;

        Assert.True(opts.IsOrdered);
        Assert.False(opts.BypassDocumentValidation);
        Assert.Equal("majority", opts.WriteConcern);
        Assert.Equal(500, opts.BatchSize);
        Assert.True(opts.UseTransaction);
    }

    [Fact]
    public void MongoDbBulkOperationOptions_CanAssignProperties()
    {
        var opts = new MongoDbBulkOperationOptions
        {
            BatchSize = 2000,
            UseTransaction = false,
            TimeoutSeconds = 600,
            IsOrdered = false,
            BypassDocumentValidation = true,
            WriteConcern = "w1",
            MaxRetryAttempts = 5,
            RetryDelayMilliseconds = 200
        };

        Assert.Equal(2000, opts.BatchSize);
        Assert.False(opts.UseTransaction);
        Assert.Equal(600, opts.TimeoutSeconds);
        Assert.False(opts.IsOrdered);
        Assert.True(opts.BypassDocumentValidation);
        Assert.Equal("w1", opts.WriteConcern);
        Assert.Equal(5, opts.MaxRetryAttempts);
        Assert.Equal(200, opts.RetryDelayMilliseconds);
    }

    [Fact]
    public void MongoDbBulkOperationOptions_HighThroughput_IsNewInstanceEachTime()
    {
        MongoDbBulkOperationOptions opts1 = MongoDbBulkOperationOptions.HighThroughput;
        MongoDbBulkOperationOptions opts2 = MongoDbBulkOperationOptions.HighThroughput;

        Assert.NotSame(opts1, opts2);
    }

    #endregion

    #region [ MongoDbRepositoryOptions ]

    [Fact]
    public void MongoDbRepositoryOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbRepositoryOptions();
        Assert.True(opts.MaxQtyByQueryPage > 0);
    }

    [Fact]
    public void MongoDbRepositoryOptions_CanAssignMaxQty()
    {
        var opts = new MongoDbRepositoryOptions { MaxQtyByQueryPage = 500 };
        Assert.Equal(500, opts.MaxQtyByQueryPage);
    }

    [Fact]
    public void MongoDbRepositoryOptions_IsSealed()
    {
        Assert.True(typeof(MongoDbRepositoryOptions).IsSealed);
    }

    #endregion

    #region [ MongoDbConnectionPoolOptions ]

    [Fact]
    public void MongoDbConnectionPoolOptions_DefaultValues_AreCorrect()
    {
        var opts = new MongoDbConnectionPoolOptions();

        Assert.Equal(0, opts.MinPoolSize);
        Assert.Equal(100, opts.MaxPoolSize);
        Assert.Equal(120, opts.WaitQueueTimeoutSeconds);
        Assert.Equal(600, opts.MaxConnectionIdleTimeSeconds);
        Assert.Equal(1800, opts.MaxConnectionLifetimeSeconds);
        Assert.Equal(30, opts.ConnectTimeoutSeconds);
        Assert.Equal(0, opts.SocketTimeoutSeconds);
        Assert.Equal(30, opts.ServerSelectionTimeoutSeconds);
        Assert.Equal(10, opts.HeartbeatFrequencySeconds);
        Assert.False(opts.IPv6);
        Assert.False(opts.DirectConnection);
        Assert.Null(opts.Compressors);
        Assert.Equal(15, opts.LocalThresholdMilliseconds);
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_CanAssignProperties()
    {
        var opts = new MongoDbConnectionPoolOptions
        {
            MinPoolSize = 10,
            MaxPoolSize = 200,
            WaitQueueTimeoutSeconds = 60,
            MaxConnectionIdleTimeSeconds = 300,
            MaxConnectionLifetimeSeconds = 900,
            ConnectTimeoutSeconds = 15,
            SocketTimeoutSeconds = 30,
            ServerSelectionTimeoutSeconds = 15,
            HeartbeatFrequencySeconds = 5,
            IPv6 = true,
            DirectConnection = true,
            Compressors = ["zstd"],
            LocalThresholdMilliseconds = 20
        };

        Assert.Equal(10, opts.MinPoolSize);
        Assert.Equal(200, opts.MaxPoolSize);
        Assert.Equal(60, opts.WaitQueueTimeoutSeconds);
        Assert.Equal(300, opts.MaxConnectionIdleTimeSeconds);
        Assert.Equal(900, opts.MaxConnectionLifetimeSeconds);
        Assert.Equal(15, opts.ConnectTimeoutSeconds);
        Assert.Equal(30, opts.SocketTimeoutSeconds);
        Assert.Equal(15, opts.ServerSelectionTimeoutSeconds);
        Assert.Equal(5, opts.HeartbeatFrequencySeconds);
        Assert.True(opts.IPv6);
        Assert.True(opts.DirectConnection);
        Assert.Equal("zstd", opts.Compressors![0]);
        Assert.Equal(20, opts.LocalThresholdMilliseconds);
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_ApplyTo_NullSettings_DoesNotThrow()
    {
        var opts = new MongoDbConnectionPoolOptions();
        Exception ex = Record.Exception(() => opts.ApplyTo(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_ApplyTo_ValidSettings_ConfiguresAll()
    {
        var opts = new MongoDbConnectionPoolOptions
        {
            MinPoolSize = 5,
            MaxPoolSize = 50,
            WaitQueueTimeoutSeconds = 30,
            MaxConnectionIdleTimeSeconds = 120,
            MaxConnectionLifetimeSeconds = 600,
            ConnectTimeoutSeconds = 10,
            ServerSelectionTimeoutSeconds = 15,
            HeartbeatFrequencySeconds = 5,
            LocalThresholdMilliseconds = 20,
            IPv6 = true,
            DirectConnection = true
        };

        var settings = new MongoClientSettings();
        opts.ApplyTo(settings);

        Assert.Equal(5, settings.MinConnectionPoolSize);
        Assert.Equal(50, settings.MaxConnectionPoolSize);
        Assert.Equal(TimeSpan.FromSeconds(30), settings.WaitQueueTimeout);
        Assert.Equal(TimeSpan.FromSeconds(120), settings.MaxConnectionIdleTime);
        Assert.Equal(TimeSpan.FromSeconds(600), settings.MaxConnectionLifeTime);
        Assert.Equal(TimeSpan.FromSeconds(10), settings.ConnectTimeout);
        Assert.Equal(TimeSpan.FromSeconds(15), settings.ServerSelectionTimeout);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.HeartbeatInterval);
        Assert.Equal(TimeSpan.FromMilliseconds(20), settings.LocalThreshold);
        Assert.True(settings.IPv6);
        Assert.True(settings.DirectConnection);
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_ApplyTo_WithSocketTimeout_SetsSocketTimeout()
    {
        var opts = new MongoDbConnectionPoolOptions { SocketTimeoutSeconds = 45 };
        var settings = new MongoClientSettings();
        opts.ApplyTo(settings);
        Assert.Equal(TimeSpan.FromSeconds(45), settings.SocketTimeout);
    }

    [Fact]
    public void MongoDbConnectionPoolOptions_ApplyTo_WithZeroSocketTimeout_DoesNotSetSocketTimeout()
    {
        var opts = new MongoDbConnectionPoolOptions { SocketTimeoutSeconds = 0 };
        var settings = new MongoClientSettings();
        TimeSpan before = settings.SocketTimeout;
        opts.ApplyTo(settings);
        Assert.Equal(before, settings.SocketTimeout);
    }

    #endregion
}
