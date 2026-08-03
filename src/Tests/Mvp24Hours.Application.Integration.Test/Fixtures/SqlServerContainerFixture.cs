//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Data;
using Mvp24Hours.Application.Integration.Test.Services;
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
    private readonly SemaphoreSlim _databaseLock = new(1, 1);
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
            services.AddLogging();
            services.AddDbContext<TestDbContext>(options =>
                options.UseSqlServer(ConnectionString));
            services.AddMvp24HoursDbContext<TestDbContext>();
            services.AddMvp24HoursRepositoryAsync(options => options.MaxQtyByQueryPage = 100);
            services.AddMvp24HoursBulkOperationsRepositoryAsync(options => options.MaxQtyByQueryPage = 100);
            services.AddScoped<ProductService>();
            services.AddScoped<ProductPagingService>();
            services.AddScoped<CategoryService>();

            ServiceProvider = services.BuildServiceProvider();

            using IServiceScope scope = ServiceProvider.CreateScope();
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureCreatedAsync().ConfigureAwait(false);
            IsAvailable = true;
        }
        catch (Exception)
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
    public Task ClearDatabaseAsync()
    {
        return ExecuteWithDatabaseLockAsync(ClearDatabaseCoreAsync);
    }

    /// <summary>
    /// Atomically clears the database and runs a seed action under the same lock.
    /// </summary>
    public Task ResetDatabaseAsync(Func<Task> seedAsync)
    {
        return ExecuteWithDatabaseLockAsync(async () =>
        {
            await ClearDatabaseCoreAsync().ConfigureAwait(false);
            await seedAsync().ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Runs an action under an exclusive database lock (clear + seed must use this to stay atomic).
    /// </summary>
    public async Task ExecuteWithDatabaseLockAsync(Func<Task> action)
    {
        if (!IsAvailable)
        {
            await action().ConfigureAwait(false);
            return;
        }

        await _databaseLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _databaseLock.Release();
        }
    }

    private async Task ClearDatabaseCoreAsync()
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
}

/// <summary>
/// Collection definition for SQL Server tests.
/// </summary>
[CollectionDefinition("SqlServer", DisableParallelization = true)]
public class SqlServerCollectionDefinition : ICollectionFixture<SqlServerContainerFixture>
{
}
