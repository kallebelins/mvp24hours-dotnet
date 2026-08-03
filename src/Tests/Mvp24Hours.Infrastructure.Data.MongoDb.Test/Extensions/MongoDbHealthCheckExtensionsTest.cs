using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbHealthCheckExtensionsTest
{
    [Fact]
    public void AddMongoDbHealthCheck_FromDiOptions_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = "mongodb://127.0.0.1:27017";
            options.DatabaseName = "HealthDb";
        });

        services.AddHealthChecks()
            .AddMongoDbHealthCheck(
                name: "mongodb-di",
                configureOptions: options =>
                {
                    options.VerifyDatabaseAccess = true;
                    options.IncludeServerStatus = true;
                },
                tags: ["database", "nosql"]);

        using ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService healthChecks = provider.GetRequiredService<HealthCheckService>();

        healthChecks.Should().NotBeNull();
        provider.GetRequiredService<IOptions<MongoDbHealthCheckOptions>>().Value.VerifyDatabaseAccess.Should().BeTrue();
    }

    [Fact]
    public void AddMongoDbHealthCheck_WithConnectionString_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddHealthChecks()
            .AddMongoDbHealthCheck(
                connectionString: "mongodb://127.0.0.1:27017",
                databaseName: "ExplicitDb",
                name: "mongodb-explicit",
                configureOptions: options => options.ConnectionTimeoutSeconds = 5,
                failureStatus: HealthStatus.Degraded,
                tags: ["database"]);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<HealthCheckService>().Should().NotBeNull();
    }

    [Fact]
    public void AddMongoDbReplicaSetHealthCheck_FromDiOptions_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = "mongodb://127.0.0.1:27017/?replicaSet=rs0";
            options.DatabaseName = "ReplicaDb";
        });

        services.AddHealthChecks()
            .AddMongoDbReplicaSetHealthCheck(
                name: "mongodb-replica-di",
                configureOptions: options =>
                {
                    options.MinSecondaryNodes = 1;
                    options.MaxReplicationLagSeconds = 30;
                    options.AllowStandaloneMode = true;
                },
                tags: ["database", "cluster"]);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<HealthCheckService>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<MongoDbReplicaSetHealthCheckOptions>>().Value.MinSecondaryNodes.Should().Be(1);
    }

    [Fact]
    public void AddMongoDbReplicaSetHealthCheck_WithConnectionString_ShouldRegisterHealthCheck()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddHealthChecks()
            .AddMongoDbReplicaSetHealthCheck(
                connectionString: "mongodb://127.0.0.1:27017/?replicaSet=rs0",
                name: "mongodb-replica-explicit",
                configureOptions: options => options.IncludeMemberDetails = true,
                failureStatus: HealthStatus.Unhealthy,
                tags: ["cluster"]);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<HealthCheckService>().Should().NotBeNull();
    }

    [Fact]
    public void AddMongoDbHealthChecks_ShouldRegisterConnectivityAndReplicaSetChecks()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<MongoDbOptions>(options =>
        {
            options.ConnectionString = "mongodb://127.0.0.1:27017";
            options.DatabaseName = "CombinedDb";
        });

        services.AddHealthChecks()
            .AddMongoDbHealthChecks(
                connectivityName: "mongodb-connectivity",
                replicaSetName: "mongodb-replica-set",
                configureConnectivity: options => options.IncludeServerStatus = true,
                configureReplicaSet: options => options.AllowUnhealthyMembers = false,
                failureStatus: HealthStatus.Degraded,
                tags: ["database"]);

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<HealthCheckService>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<MongoDbHealthCheckOptions>>().Value.IncludeServerStatus.Should().BeTrue();
        provider.GetRequiredService<IOptions<MongoDbReplicaSetHealthCheckOptions>>().Value.AllowUnhealthyMembers.Should().BeFalse();
    }
}
