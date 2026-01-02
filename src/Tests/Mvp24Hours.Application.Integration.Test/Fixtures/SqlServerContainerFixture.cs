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
    private readonly MsSqlContainer _container;
    private IServiceProvider _serviceProvider = null!;
    private string _connectionString = null!;

    public SqlServerContainerFixture()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("YourStrong@Passw0rd!")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Developer")
            .Build();
    }

    public IServiceProvider ServiceProvider => _serviceProvider;
    public string ConnectionString => _connectionString;

    public async Task InitializeAsync()
    {
        // Start the SQL Server container
        await _container.StartAsync();

        // Build connection string with dedicated database
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = DatabaseName,
            TrustServerCertificate = true
        };
        _connectionString = builder.ConnectionString;

        // Configure services
        var services = new ServiceCollection();

        // Add DbContext with SQL Server
        services.AddDbContext<TestDbContext>(options =>
            options.UseSqlServer(_connectionString));

        // Add Mvp24Hours EFCore integration
        services.AddMvp24HoursDbContext<TestDbContext>();
        services.AddMvp24HoursRepositoryAsync(options =>
        {
            options.MaxQtyByQueryPage = 100;
        });

        // Register services
        services.AddScoped<ProductService>();
        services.AddScoped<ProductPagingService>();
        services.AddScoped<CategoryService>();

        _serviceProvider = services.BuildServiceProvider();

        // Ensure database is created
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        // Simply stop and dispose container - no need to delete database
        // as the container will be destroyed anyway
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Creates a new scope for test isolation.
    /// </summary>
    public IServiceScope CreateScope() => _serviceProvider.CreateScope();

    /// <summary>
    /// Clears all data from the database (for test isolation).
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Remove all entities (order matters for foreign keys)
        dbContext.Products.RemoveRange(dbContext.Products);
        dbContext.Categories.RemoveRange(dbContext.Categories);
        await dbContext.SaveChangesAsync();
    }
}

/// <summary>
/// Collection definition for SQL Server tests.
/// </summary>
[CollectionDefinition("SqlServer")]
public class SqlServerCollectionDefinition : ICollectionFixture<SqlServerContainerFixture>
{
}

