//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Integration.Test.Data;
using Mvp24Hours.Application.Integration.Test.Services;
using Mvp24Hours.Extensions;
using Testcontainers.PostgreSql;

namespace Mvp24Hours.Application.Integration.Test.Fixtures;

/// <summary>
/// PostgreSQL Testcontainers fixture for integration tests.
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private const string DatabaseName = "Mvp24HoursIntegrationTest";
    private PostgreSqlContainer? _container;

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
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase(DatabaseName)
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync().ConfigureAwait(false);
            ConnectionString = _container.GetConnectionString();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TestDbContext>(options =>
                options.UseNpgsql(ConnectionString));
            services.AddMvp24HoursDbContext<TestDbContext>();
            services.AddMvp24HoursRepositoryAsync(options => options.MaxQtyByQueryPage = 100);
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

    public IServiceScope CreateScope()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(DockerAvailability.SkipReason);
        }

        return ServiceProvider.CreateScope();
    }

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
}

[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollectionDefinition : ICollectionFixture<PostgreSqlContainerFixture>
{
}
