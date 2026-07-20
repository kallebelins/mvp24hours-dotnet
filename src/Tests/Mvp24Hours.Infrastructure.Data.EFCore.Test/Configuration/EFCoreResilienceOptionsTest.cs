using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Configuration;

[Trait("Category", "Unit")]
public class EFCoreResilienceOptionsTest
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var options = new EFCoreResilienceOptions();

        options.EnableRetryOnFailure.Should().BeTrue();
        options.MaxRetryCount.Should().Be(6);
        options.MaxRetryDelaySeconds.Should().Be(30);
        options.AdditionalTransientErrorNumbers.Should().BeEmpty();
        options.TransientExceptionTypes.Should().BeEmpty();
        options.CommandTimeoutSeconds.Should().Be(30);
        options.ReadCommandTimeoutSeconds.Should().BeNull();
        options.WriteCommandTimeoutSeconds.Should().BeNull();
        options.BulkCommandTimeoutSeconds.Should().Be(120);
        options.MigrationCommandTimeoutSeconds.Should().Be(300);
        options.EnableDbContextPooling.Should().BeTrue();
        options.PoolSize.Should().Be(1024);
        options.EnableCircuitBreaker.Should().BeFalse();
        options.CircuitBreakerFailureThreshold.Should().Be(5);
        options.CircuitBreakerDurationSeconds.Should().Be(30);
        options.LogRetryAttempts.Should().BeTrue();
        options.LogPoolStatistics.Should().BeFalse();
        options.PoolStatisticsLogIntervalSeconds.Should().Be(60);
    }

    [Fact]
    public void GetReadTimeout_ShouldUseReadOverrideOrDefault()
    {
        var withDefault = new EFCoreResilienceOptions { CommandTimeoutSeconds = 40 };
        var withOverride = new EFCoreResilienceOptions
        {
            CommandTimeoutSeconds = 40,
            ReadCommandTimeoutSeconds = 15
        };

        withDefault.GetReadTimeout().Should().Be(40);
        withOverride.GetReadTimeout().Should().Be(15);
    }

    [Fact]
    public void GetWriteTimeout_ShouldUseWriteOverrideOrDefault()
    {
        var withDefault = new EFCoreResilienceOptions { CommandTimeoutSeconds = 40 };
        var withOverride = new EFCoreResilienceOptions
        {
            CommandTimeoutSeconds = 40,
            WriteCommandTimeoutSeconds = 20
        };

        withDefault.GetWriteTimeout().Should().Be(40);
        withOverride.GetWriteTimeout().Should().Be(20);
    }

    [Fact]
    public void Production_ShouldReturnProductionDefaults()
    {
        EFCoreResilienceOptions options = EFCoreResilienceOptions.Production();

        options.EnableRetryOnFailure.Should().BeTrue();
        options.MaxRetryCount.Should().Be(6);
        options.MaxRetryDelaySeconds.Should().Be(30);
        options.CommandTimeoutSeconds.Should().Be(30);
        options.EnableDbContextPooling.Should().BeTrue();
        options.PoolSize.Should().Be(1024);
        options.LogRetryAttempts.Should().BeTrue();
        options.LogPoolStatistics.Should().BeFalse();
    }

    [Fact]
    public void Development_ShouldReturnDevelopmentDefaults()
    {
        EFCoreResilienceOptions options = EFCoreResilienceOptions.Development();

        options.EnableRetryOnFailure.Should().BeTrue();
        options.MaxRetryCount.Should().Be(3);
        options.MaxRetryDelaySeconds.Should().Be(5);
        options.CommandTimeoutSeconds.Should().Be(60);
        options.EnableDbContextPooling.Should().BeFalse();
        options.LogRetryAttempts.Should().BeTrue();
        options.LogPoolStatistics.Should().BeTrue();
        options.PoolStatisticsLogIntervalSeconds.Should().Be(30);
    }

    [Fact]
    public void AzureSql_ShouldReturnAzureDefaults()
    {
        EFCoreResilienceOptions options = EFCoreResilienceOptions.AzureSql();

        options.EnableRetryOnFailure.Should().BeTrue();
        options.MaxRetryCount.Should().Be(10);
        options.MaxRetryDelaySeconds.Should().Be(60);
        options.CommandTimeoutSeconds.Should().Be(60);
        options.EnableDbContextPooling.Should().BeTrue();
        options.PoolSize.Should().Be(512);
        options.EnableCircuitBreaker.Should().BeTrue();
        options.CircuitBreakerFailureThreshold.Should().Be(5);
        options.CircuitBreakerDurationSeconds.Should().Be(60);
        options.AdditionalTransientErrorNumbers.Should().Contain([4060, 4221, 40143, 40615]);
    }

    [Fact]
    public void NoResilience_ShouldDisableResilienceFeatures()
    {
        EFCoreResilienceOptions options = EFCoreResilienceOptions.NoResilience();

        options.EnableRetryOnFailure.Should().BeFalse();
        options.EnableDbContextPooling.Should().BeFalse();
        options.EnableCircuitBreaker.Should().BeFalse();
        options.LogRetryAttempts.Should().BeFalse();
    }
}
