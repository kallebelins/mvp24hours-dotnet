using Testcontainers.MsSql;

namespace CustomerAPI.Test.Support;

/// <summary>
/// Shared SQL Server Testcontainer for CQRS integration tests.
/// </summary>
public sealed class SqlServerContainerFixture : IAsyncLifetime
{
    private const string DatabaseName = "CustomerCqrsTest";
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .WithPassword("YourStrong@Passw0rd!")
        .WithEnvironment("ACCEPT_EULA", "Y")
        .WithEnvironment("MSSQL_PID", "Developer")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public bool IsAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            return;
        }

        try
        {
            await _container.StartAsync().ConfigureAwait(false);

            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
                TrustServerCertificate = true
            };
            ConnectionString = builder.ConnectionString;
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (IsAvailable)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }
}

[CollectionDefinition("SqlServer", DisableParallelization = true)]
public sealed class SqlServerCollectionDefinition : ICollectionFixture<SqlServerContainerFixture>
{
}
