//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using DotNet.Testcontainers.Builders;
using Mvp24Hours.Application.Integration.Test.Data;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Application.Integration.Test.Support;
using Mvp24Hours.Extensions;
using Testcontainers.MsSql;

namespace Mvp24Hours.Application.Integration.Test.Fixtures;

/// <summary>
/// SQL Server Testcontainers fixture for integration tests.
/// Provides a real SQL Server database instance running in Docker.
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{
    private const string DatabaseName = "Mvp24HoursIntegrationTest";
    private MsSqlContainer? _container;

    public IServiceProvider ServiceProvider { get; private set; } = null!;
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
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
                .WithPassword("YourStrong@Passw0rd!")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_PID", "Developer")
                .Build();

            await _container.StartAsync().ConfigureAwait(false);

            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
            {
                InitialCatalog = DatabaseName,
                TrustServerCertificate = true
            };
            ConnectionString = builder.ConnectionString;

            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(options =>
                options.UseSqlServer(ConnectionString));
            services.AddMvp24HoursDbContext<TestDbContext>();
            services.AddMvp24HoursRepositoryAsync(options => options.MaxQtyByQueryPage = 100);
            services.AddScoped<ProductService>();
            services.AddScoped<ProductPagingService>();
            services.AddScoped<CategoryService>();

            ServiceProvider = services.BuildServiceProvider();

            using IServiceScope scope = ServiceProvider.CreateScope();
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            IsAvailable = true;
        }
        catch (Exception ex) when (IsDockerUnavailable(ex))
        {
            IsAvailable = false;
            ConnectionString = string.Empty;
            ServiceProvider = new ServiceCollection().BuildServiceProvider();
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync().ConfigureAwait(false);
            await _container.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a new scope for test isolation.
    /// </summary>
    public IServiceScope CreateScope()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(DockerAvailability.SkipReason);
        }

        return ServiceProvider.CreateScope();
    }

    /// <summary>
    /// Clears all data from the database (for test isolation).
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        using IServiceScope scope = ServiceProvider.CreateScope();
        TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        dbContext.Products.RemoveRange(dbContext.Products);
        dbContext.Categories.RemoveRange(dbContext.Categories);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static bool IsDockerUnavailable(Exception ex)
    {
        if (ex is DockerUnavailableException)
        {
            return true;
        }

        if (ex is AggregateException aggregate)
        {
            return aggregate.Flatten().InnerExceptions.Any(static e => e is DockerUnavailableException);
        }

        return false;
    }
}

/// <summary>
/// Collection definition for SQL Server tests.
/// </summary>
[CollectionDefinition("SqlServer")]
public class SqlServerCollectionDefinition : ICollectionFixture<SqlServerContainerFixture>
{
}
