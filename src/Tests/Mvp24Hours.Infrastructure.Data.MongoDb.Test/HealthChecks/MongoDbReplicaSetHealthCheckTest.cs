using System.Net;
using System.Reflection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;
using Moq;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.HealthChecks;

[Trait("Category", "Unit")]
public class MongoDbReplicaSetHealthCheckTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrow()
    {
        Action act = () => new MongoDbReplicaSetHealthCheck((IOptions<MongoDbOptions>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullMongoDbOptions_ShouldThrow()
    {
        Action act = () => new MongoDbReplicaSetHealthCheck((MongoDbOptions)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CheckHealthAsync_WithHealthyReplicaSet_ShouldReturnHealthy()
    {
        DateTime primaryOptime = DateTime.UtcNow.AddSeconds(-2);
        DateTime secondaryOptime = DateTime.UtcNow.AddSeconds(-5);

        BsonDocument replSetStatus = new()
        {
            { "set", "rs-test" },
            {
                "members", new BsonArray
                {
                    CreateMember(0, "host1:27017", 1, "PRIMARY", 1, true, primaryOptime),
                    CreateMember(1, "host2:27017", 2, "SECONDARY", 1, false, secondaryOptime)
                }
            }
        };

        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithResponse(replSetStatus);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("rs-test");
        result.Data["primaryCount"].Should().Be(1);
        result.Data["secondaryCount"].Should().Be(1);
        result.Data["members"].Should().NotBeNull();
    }

    [Fact]
    public async Task CheckHealthAsync_WithNoPrimary_ShouldReturnUnhealthy()
    {
        BsonDocument replSetStatus = new()
        {
            { "set", "rs-test" },
            {
                "members", new BsonArray
                {
                    CreateMember(1, "host2:27017", 2, "SECONDARY", 1, false, DateTime.UtcNow)
                }
            }
        };

        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithResponse(
            replSetStatus,
            new MongoDbReplicaSetHealthCheckOptions { IncludeMemberDetails = false });

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("No primary node");
    }

    [Fact]
    public async Task CheckHealthAsync_WithInsufficientSecondaries_ShouldReturnDegraded()
    {
        BsonDocument replSetStatus = new()
        {
            { "set", "rs-test" },
            {
                "members", new BsonArray
                {
                    CreateMember(0, "host1:27017", 1, "PRIMARY", 1, true, DateTime.UtcNow),
                    CreateMember(1, "host2:27017", 2, "SECONDARY", 1, false, DateTime.UtcNow)
                }
            }
        };

        var options = new MongoDbReplicaSetHealthCheckOptions
        {
            MinSecondaryNodes = 2,
            IncludeMemberDetails = false
        };

        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithResponse(replSetStatus, options);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Insufficient secondary nodes");
    }

    [Fact]
    public async Task CheckHealthAsync_WithHighReplicationLag_ShouldReturnDegraded()
    {
        DateTime primaryOptime = DateTime.UtcNow;
        DateTime secondaryOptime = DateTime.UtcNow.AddSeconds(-120);

        BsonDocument replSetStatus = new()
        {
            { "set", "rs-test" },
            {
                "members", new BsonArray
                {
                    CreateMember(0, "host1:27017", 1, "PRIMARY", 1, true, primaryOptime),
                    CreateMember(1, "host2:27017", 2, "SECONDARY", 1, false, secondaryOptime)
                }
            }
        };

        var options = new MongoDbReplicaSetHealthCheckOptions
        {
            MaxReplicationLagSeconds = 30,
            IncludeMemberDetails = false
        };

        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithResponse(replSetStatus, options);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("Replication lag");
    }

    [Fact]
    public async Task CheckHealthAsync_WithUnhealthyMembers_ShouldReturnDegradedWhenNotAllowed()
    {
        BsonDocument replSetStatus = new()
        {
            { "set", "rs-test" },
            {
                "members", new BsonArray
                {
                    CreateMember(0, "host1:27017", 1, "PRIMARY", 1, true, DateTime.UtcNow),
                    CreateMember(1, "host2:27017", 2, "SECONDARY", 0, false, DateTime.UtcNow)
                }
            }
        };

        var options = new MongoDbReplicaSetHealthCheckOptions
        {
            AllowUnhealthyMembers = false,
            IncludeMemberDetails = false
        };

        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithResponse(replSetStatus, options);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unhealthy member");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullResponse_ShouldReturnUnhealthy()
    {
        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithResponse(null);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Unable to retrieve replica set status");
    }

    [Fact]
    public async Task CheckHealthAsync_WithNotYetInitialized_ShouldReturnUnhealthy()
    {
        MongoCommandException commandException = CreateMongoCommandException(94, "NotYetInitialized");
        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithException(commandException);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["errorType"].Should().Be("NotInitialized");
    }

    [Fact]
    public async Task CheckHealthAsync_WithStandaloneModeAllowed_ShouldReturnHealthy()
    {
        MongoCommandException commandException = CreateMongoCommandException(76, "NoReplicationEnabled");
        var options = new MongoDbReplicaSetHealthCheckOptions { AllowStandaloneMode = true };
        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithException(commandException, options);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("standalone mode");
    }

    [Fact]
    public async Task CheckHealthAsync_WithStandaloneModeNotAllowed_ShouldReturnUnhealthy()
    {
        MongoCommandException commandException = CreateMongoCommandException(76, "NoReplicationEnabled");
        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithException(commandException);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["errorType"].Should().Be("StandaloneMode");
    }

    [Fact]
    public async Task CheckHealthAsync_WithGenericException_ShouldReturnUnhealthy()
    {
        MongoDbReplicaSetHealthCheck healthCheck = CreateHealthCheckWithException(new InvalidOperationException("connection failed"));

        HealthCheckResult result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data["errorType"].Should().Be(nameof(InvalidOperationException));
    }

    private static BsonDocument CreateMember(
        int id,
        string name,
        int state,
        string stateStr,
        double health,
        bool self,
        DateTime optimeDate)
    {
        return new BsonDocument
        {
            { "_id", id },
            { "name", name },
            { "state", state },
            { "stateStr", stateStr },
            { "health", health },
            { "self", self },
            { "optimeDate", optimeDate }
        };
    }

    private static MongoDbReplicaSetHealthCheck CreateHealthCheckWithResponse(
        BsonDocument? response,
        MongoDbReplicaSetHealthCheckOptions? healthCheckOptions = null)
    {
        var mockDatabase = new Mock<IMongoDatabase>();
        mockDatabase
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response!);

        var mockClient = new Mock<IMongoClient>();
        mockClient.Setup(c => c.GetDatabase("admin", null)).Returns(mockDatabase.Object);

        var mongoOptions = new MongoDbOptions { ConnectionString = "mongodb://localhost:27017" };
        var healthCheck = new MongoDbReplicaSetHealthCheck(mongoOptions, healthCheckOptions);

        SetPrivateField(healthCheck, "_client", mockClient.Object);
        return healthCheck;
    }

    private static MongoDbReplicaSetHealthCheck CreateHealthCheckWithException(
        Exception exception,
        MongoDbReplicaSetHealthCheckOptions? healthCheckOptions = null)
    {
        var mockDatabase = new Mock<IMongoDatabase>();
        mockDatabase
            .Setup(d => d.RunCommandAsync(
                It.IsAny<Command<BsonDocument>>(),
                It.IsAny<ReadPreference>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var mockClient = new Mock<IMongoClient>();
        mockClient.Setup(c => c.GetDatabase("admin", null)).Returns(mockDatabase.Object);

        var healthCheck = new MongoDbReplicaSetHealthCheck(
            new MongoDbOptions { ConnectionString = "mongodb://localhost:27017" },
            healthCheckOptions);

        SetPrivateField(healthCheck, "_client", mockClient.Object);
        return healthCheck;
    }

    private static MongoCommandException CreateMongoCommandException(int code, string codeName)
    {
        var connectionId = new ConnectionId(new ServerId(new ClusterId(1), new DnsEndPoint("localhost", 27017)), 1);
        var result = new BsonDocument { { "code", code }, { "codeName", codeName } };
        return new MongoCommandException(connectionId, codeName, [], result);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo? field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull();
        field!.SetValue(target, value);
    }
}
