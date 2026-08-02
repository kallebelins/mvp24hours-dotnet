using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Data.EFCore.ReadWriteSplitting;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.ReadWriteSplitting;

[Trait("Category", "Unit")]
public class ConnectionResolverTest
{
    private const string Primary = "Server=primary;Database=App;";
    private const string Replica = "Server=replica;Database=App;";

    [Fact]
    public void GetWriteConnectionString_ShouldReturnPrimary()
    {
        ConnectionResolver resolver = CreateResolver(CreateReplicaSelectorMock());

        resolver.GetWriteConnectionString().Should().Be(Primary);
    }

    [Fact]
    public async Task GetReadConnectionStringAsync_WithNoReplicas_ShouldReturnPrimary()
    {
        Mock<IReplicaSelector> selector = CreateReplicaSelectorMock(replicas: []);
        ConnectionResolver resolver = CreateResolver(selector, replicas: []);

        string readConnection = await resolver.GetReadConnectionStringAsync();

        readConnection.Should().Be(Primary);
    }

    [Fact]
    public async Task GetReadConnectionStringAsync_WithReplica_ShouldReturnReplica()
    {
        Mock<IReplicaSelector> selector = CreateReplicaSelectorMock();
        selector.Setup(x => x.SelectReplicaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Replica);

        ConnectionResolver resolver = CreateResolver(selector);

        string readConnection = await resolver.GetReadConnectionStringAsync();

        readConnection.Should().Be(Replica);
    }

    [Fact]
    public async Task NotifyWritePerformed_WithReadAfterWriteEnabled_ShouldStickReadToPrimary()
    {
        Mock<IReplicaSelector> selector = CreateReplicaSelectorMock();
        selector.Setup(x => x.SelectReplicaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Replica);

        ConnectionResolver resolver = CreateResolver(selector, replicas: [Replica], configure: options =>
        {
            options.EnableReadAfterWriteConsistency = true;
            options.ReadAfterWriteWindow = TimeSpan.FromSeconds(30);
        });

        resolver.NotifyWritePerformed();

        string readConnection = await resolver.GetReadConnectionStringAsync();

        readConnection.Should().Be(Primary);
    }

    [Fact]
    public async Task ForceReadFromPrimary_ShouldReturnPrimaryUntilReset()
    {
        Mock<IReplicaSelector> selector = CreateReplicaSelectorMock();
        selector.Setup(x => x.SelectReplicaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Replica);

        ConnectionResolver resolver = CreateResolver(selector);

        resolver.ForceReadFromPrimary();
        (await resolver.GetReadConnectionStringAsync()).Should().Be(Primary);

        resolver.ResetReadFromPrimary();
        (await resolver.GetReadConnectionStringAsync()).Should().Be(Replica);
    }

    private static Mock<IReplicaSelector> CreateReplicaSelectorMock(IList<string>? replicas = null)
    {
        var mock = new Mock<IReplicaSelector>();
        mock.Setup(x => x.GetPrimaryConnectionString()).Returns(Primary);
        mock.Setup(x => x.SelectReplicaAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(replicas?.FirstOrDefault() ?? Replica);
        return mock;
    }

    private static ConnectionResolver CreateResolver(
        Mock<IReplicaSelector> selector,
        IList<string>? replicas = null,
        Action<ReadWriteOptions>? configure = null)
    {
        var options = new ReadWriteOptions
        {
            PrimaryConnectionString = Primary,
            ReplicaConnectionStrings = replicas ?? [Replica]
        };
        configure?.Invoke(options);

        ILogger<ConnectionResolver> logger = new LoggerFactory().CreateLogger<ConnectionResolver>();
        return new ConnectionResolver(selector.Object, Options.Create(options), logger);
    }
}
