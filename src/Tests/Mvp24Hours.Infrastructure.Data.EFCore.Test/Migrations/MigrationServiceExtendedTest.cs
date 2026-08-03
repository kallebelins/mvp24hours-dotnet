using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationServiceExtendedTest
{
    [Fact]
    public async Task ValidateSchemaAsync_WithUnmappedEntity_ShouldReturnInvalid()
    {
        await using InvalidSchemaDbContext context = CreateInvalidSchemaContext();
        MigrationService<InvalidSchemaDbContext> service = CreateService(context);

        SchemaValidationResult result = await service.ValidateSchemaAsync();

        result.IsValid.Should().BeFalse();
        result.Differences.Should().Contain(d => d.Type == SchemaDifferenceType.MissingTable);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_WhenDatabaseDoesNotExist_ShouldCreateDatabase()
    {
        string connectionString = $"Data Source=file:create_{Guid.NewGuid():N}.db";
        await using var connection = new SqliteConnection(connectionString);
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new TestDbContext(options);
        MigrationService<TestDbContext> service = CreateService(context);

        bool created = await service.EnsureDatabaseCreatedAsync();

        created.Should().BeTrue();
        (await context.Database.CanConnectAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task MigrateAsync_WithEnsureDatabaseCreated_ShouldSucceedOnSqlite()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = new(
            context,
            Options.Create(new MigrationOptions { EnsureDatabaseCreated = true }),
            new LoggerFactory().CreateLogger<MigrationService<TestDbContext>>());

        MigrationResult result = await service.MigrateAsync();

        result.Success.Should().BeTrue();
    }

    private static MigrationService<TContext> CreateService<TContext>(TContext context)
        where TContext : DbContext
    {
        return new MigrationService<TContext>(
            context,
            Options.Create(new MigrationOptions()),
            new LoggerFactory().CreateLogger<MigrationService<TContext>>());
    }

    private static TestDbContext CreateSqliteContext()
    {
        string connectionString = $"Data Source=file:migration_ext_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(keepAlive)
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static InvalidSchemaDbContext CreateInvalidSchemaContext()
    {
        DbContextOptions<InvalidSchemaDbContext> options = new DbContextOptionsBuilder<InvalidSchemaDbContext>()
            .UseInMemoryDatabase($"InvalidSchema_{Guid.NewGuid():N}")
            .Options;

        var context = new InvalidSchemaDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
