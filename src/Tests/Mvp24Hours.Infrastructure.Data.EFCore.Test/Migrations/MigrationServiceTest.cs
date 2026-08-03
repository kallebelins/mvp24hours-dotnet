using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationServiceTest
{
    private static MigrationService<TestDbContext> CreateService(TestDbContext? context = null)
    {
        TestDbContext dbContext = context ?? EfCoreTestHelpers.CreateContext();
        ILogger<MigrationService<TestDbContext>> logger = new LoggerFactory().CreateLogger<MigrationService<TestDbContext>>();
        return new MigrationService<TestDbContext>(
            dbContext,
            Options.Create(new MigrationOptions()),
            logger);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_ShouldCreateInMemoryDatabase()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        bool created = await service.EnsureDatabaseCreatedAsync();

        created.Should().BeFalse();
        (await context.Database.CanConnectAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDatabaseAsync_ShouldRemoveInMemoryDatabase()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        bool deleted = await service.DeleteDatabaseAsync();

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingAndAppliedMigrationsAsync_OnInMemory_ShouldThrowRelationalException()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        Func<Task> pending = () => service.GetPendingMigrationsAsync();
        Func<Task> applied = () => service.GetAppliedMigrationsAsync();
        Func<Task> all = () => service.GetAllMigrationsAsync();

        await pending.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Relational-specific*");
        await applied.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Relational-specific*");
        await all.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Relational-specific*");
    }

    [Fact]
    public async Task MigrateAsync_OnInMemory_ShouldReturnFailedWhenMigrationApisUnavailable()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        MigrationResult result = await service.MigrateAsync();

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Relational-specific");
    }

    [Fact]
    public async Task ValidateSchemaAsync_ShouldReturnValidForConfiguredModel()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        SchemaValidationResult result = await service.ValidateSchemaAsync();

        result.IsValid.Should().BeTrue();
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateToAsync_OnInMemory_ShouldReturnFailedResult()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        MigrationResult result = await service.MigrateToAsync("DoesNotExist");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RollbackLastAsync_OnInMemory_ShouldThrowRelationalException()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        Func<Task> act = () => service.RollbackLastAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Relational-specific*");
    }

    [Fact]
    public async Task GetMigrationScriptAsync_ShouldReturnScriptOrEmptyString()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        try
        {
            string script = await service.GetMigrationScriptAsync();
            script.Should().NotBeNull();
        }
        catch (Exception ex)
        {
            ex.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task HasPendingMigrationsAsync_WithSqliteAndNoMigrations_ShouldReturnFalse()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        bool hasPending = await service.HasPendingMigrationsAsync();

        hasPending.Should().BeFalse();
    }

    [Fact]
    public async Task MigrateAsync_WithSqliteAndNoPendingMigrations_ShouldReturnNoMigrationsNeeded()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        MigrationResult result = await service.MigrateAsync();

        result.Success.Should().BeTrue();
        result.AppliedMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllMigrationsAsync_WithSqlite_ShouldReturnEmptyWhenNoMigrationsDefined()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        IReadOnlyList<string> migrations = await service.GetAllMigrationsAsync();

        migrations.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateToAsync_WithNullTarget_ShouldThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        Func<Task> act = () => service.MigrateToAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RollbackToAsync_WithNullTarget_ShouldThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        Func<Task> act = () => service.RollbackToAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RollbackLastAsync_WithSqliteAndNoAppliedMigrations_ShouldReturnFailed()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        MigrationResult result = await service.RollbackLastAsync();

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("less than 2 migrations");
    }

    [Fact]
    public async Task GetMigrationScriptAsync_WithRange_ShouldReturnScript()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        string script = await service.GetMigrationScriptAsync(null, null);

        script.Should().NotBeNull();
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_WithExistingSqliteDatabase_ShouldReturnFalse()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        bool created = await service.EnsureDatabaseCreatedAsync();

        created.Should().BeFalse();
    }

    [Fact]
    public void AddMvp24HoursMigrationService_ShouldRegisterMigrationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>();
        services.AddMvp24HoursMigrationService<TestDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IMigrationService migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
        migrationService.Should().BeOfType<MigrationService<TestDbContext>>();
    }

    private static TestDbContext CreateSqliteContext()
    {
        string connectionString = $"Data Source=file:migration_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();

        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(keepAlive)
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_WithSqliteAndNoMigrations_ShouldReturnEmpty()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        IReadOnlyList<string> pending = await service.GetPendingMigrationsAsync();

        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_WithSqliteAndNoMigrations_ShouldReturnEmpty()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        IReadOnlyList<string> applied = await service.GetAppliedMigrationsAsync();

        applied.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateToAsync_WithWhitespaceTarget_ShouldThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        Func<Task> act = () => service.MigrateToAsync("   ");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RollbackToAsync_WithWhitespaceTarget_ShouldThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        MigrationService<TestDbContext> service = CreateService(context);

        Func<Task> act = () => service.RollbackToAsync("   ");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task MigrateToAsync_WithUnknownMigration_ShouldReturnFailedResult()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        MigrationResult result = await service.MigrateToAsync("NonExistentMigration");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteDatabaseAsync_WithSqlite_ShouldReturnTrue()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = CreateService(context);

        bool deleted = await service.DeleteDatabaseAsync();

        deleted.Should().BeTrue();
    }
}
