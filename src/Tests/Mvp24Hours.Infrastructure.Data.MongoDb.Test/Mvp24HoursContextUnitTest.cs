using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Security;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Unit")]
public class Mvp24HoursContextUnitTest
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    [Fact]
    public void Constructor_WithNullOptionsFromDi_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new Mvp24HoursContext((IOptions<MongoDbOptions>)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyDatabaseName_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new Mvp24HoursContext(string.Empty, ConnectionString);
        act.Should().Throw<ArgumentNullException>().WithParameterName("databaseName");
    }

    [Fact]
    public void Constructor_WithEmptyConnectionString_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new Mvp24HoursContext("test_db", string.Empty);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [Fact]
    public void Constructor_WithMongoDbOptions_ShouldInitializeProperties()
    {
        var options = new MongoDbOptions
        {
            DatabaseName = "orders_db",
            ConnectionString = ConnectionString,
            EnableTls = true,
            EnableTransaction = true,
            EnableMultiTenancy = true,
            ReadPreference = "secondaryPreferred",
            WriteConcern = "majority",
            ReadConcern = "majority",
            RetryReads = true,
            RetryWrites = false,
            ConnectionTimeoutSeconds = 5,
            SocketTimeoutSeconds = 10,
            MaxConnectionPoolSize = 50,
            MinConnectionPoolSize = 2
        };

        using var context = new Mvp24HoursContext(options);

        context.DatabaseName.Should().Be("orders_db");
        context.ConnectionString.Should().Be(ConnectionString);
        context.EnableTls.Should().BeTrue();
        context.EnableTransaction.Should().BeTrue();
        context.EnableMultiTenancy.Should().BeTrue();
        context.Database.Should().NotBeNull();
        context.MongoClient.Should().NotBeNull();
        context.RowLevelSecurity.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_FromDiOptions_ShouldInitializeProperties()
    {
        IOptions<MongoDbOptions> options = Options.Create(new MongoDbOptions
        {
            DatabaseName = "di_db",
            ConnectionString = ConnectionString
        });

        using var context = new Mvp24HoursContext(options);

        context.DatabaseName.Should().Be("di_db");
        context.Database.DatabaseNamespace.DatabaseName.Should().Be("di_db");
    }

    [Fact]
    public void Set_ShouldReturnNamedCollection()
    {
        using var context = new Mvp24HoursContext("set_db", ConnectionString);

        IMongoCollection<ContextTestDocument> collection = context.Set<ContextTestDocument>("custom_orders");

        collection.CollectionNamespace.CollectionName.Should().Be("custom_orders");
    }

    [Fact]
    public void Set_WithoutName_ShouldUseTypeName()
    {
        using var context = new Mvp24HoursContext("set_db2", ConnectionString);

        IMongoCollection<ContextTestDocument> collection = context.Set<ContextTestDocument>();

        collection.CollectionNamespace.CollectionName.Should().Be(nameof(ContextTestDocument));
    }

    [Fact]
    public void Configure_WithAuthenticationOptions_ShouldNotThrow()
    {
        var options = new MongoDbOptions
        {
            DatabaseName = "auth_db",
            ConnectionString = ConnectionString,
            Authentication = new MongoDbAuthenticationOptions
            {
                Mechanism = MongoDbAuthMechanism.ScramSha256,
                Username = "user",
                Password = "pass",
                AuthDatabase = "admin"
            }
        };

        Action act = () =>
        {
            using var context = new Mvp24HoursContext(options);
            context.MongoClient.Should().NotBeNull();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_WithCommandLogging_ShouldNotThrow()
    {
        var options = new MongoDbOptions
        {
            DatabaseName = "logging_db",
            ConnectionString = ConnectionString,
            EnableCommandLogging = true
        };

        Action act = () =>
        {
            using var context = new Mvp24HoursContext(options);
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithoutActiveSession_ShouldNotThrow()
    {
        var context = new Mvp24HoursContext("dispose_db", ConnectionString);

        Action act = () => context.Dispose();

        act.Should().NotThrow();
    }
}

public class ContextTestDocument
{
    public ObjectId Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
