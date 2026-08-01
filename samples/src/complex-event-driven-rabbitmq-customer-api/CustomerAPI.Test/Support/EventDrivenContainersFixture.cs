using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace CustomerAPI.Test.Support;

/// <summary>
/// SQL Server + RabbitMQ Testcontainers for event-driven integration tests.
/// </summary>
public sealed class EventDrivenContainersFixture : IAsyncLifetime
{
    private const string DatabaseName = "EventDrivenTest";

    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .WithPassword("YourStrong@Passw0rd!")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_PID", "Developer")
        .Build();

    private readonly RabbitMqContainer _rabbitContainer = new RabbitMqBuilder("rabbitmq:3.13-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .WithCleanUp(true)
        .Build();

    public string SqlConnectionString { get; private set; } = string.Empty;

    public string RabbitConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        try
        {
            await Task.WhenAll(
                _sqlContainer.StartAsync(),
                _rabbitContainer.StartAsync()).ConfigureAwait(false);

            var sqlBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
                TrustServerCertificate = true
            };
            SqlConnectionString = sqlBuilder.ConnectionString;
            RabbitConnectionString = _rabbitContainer.GetConnectionString();
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await Task.WhenAll(
            _sqlContainer.DisposeAsync().AsTask(),
            _rabbitContainer.DisposeAsync().AsTask()).ConfigureAwait(false);
    }
}

[CollectionDefinition("EventDrivenContainers", DisableParallelization = true)]
public sealed class EventDrivenContainersCollectionDefinition : ICollectionFixture<EventDrivenContainersFixture>
{
}
