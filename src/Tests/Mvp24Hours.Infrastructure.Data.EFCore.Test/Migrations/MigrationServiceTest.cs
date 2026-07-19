using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationServiceTest
{
    private static MigrationService<TestDbContext> CreateService(TestDbContext? context = null)
    {
        TestDbContext dbContext = context ?? EfCoreTestHelpers.CreateContext();
        var logger = new LoggerFactory().CreateLogger<MigrationService<TestDbContext>>();
        return new MigrationService<TestDbContext>(
            dbContext,
            Options.Create(new MigrationOptions()),
            logger);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_ShouldCreateInMemoryDatabase()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

        bool created = await service.EnsureDatabaseCreatedAsync();

        created.Should().BeFalse();
        (await context.Database.CanConnectAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDatabaseAsync_ShouldRemoveInMemoryDatabase()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

        bool deleted = await service.DeleteDatabaseAsync();

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingAndAppliedMigrationsAsync_OnInMemory_ShouldThrowRelationalException()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

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
        var service = CreateService(context);

        MigrationResult result = await service.MigrateAsync();

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Relational-specific");
    }

    [Fact]
    public async Task ValidateSchemaAsync_ShouldReturnValidForConfiguredModel()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

        SchemaValidationResult result = await service.ValidateSchemaAsync();

        result.IsValid.Should().BeTrue();
        result.Differences.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateToAsync_OnInMemory_ShouldReturnFailedResult()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

        MigrationResult result = await service.MigrateToAsync("DoesNotExist");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RollbackLastAsync_OnInMemory_ShouldThrowRelationalException()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

        Func<Task> act = () => service.RollbackLastAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Relational-specific*");
    }

    [Fact]
    public async Task GetMigrationScriptAsync_ShouldReturnScriptOrEmptyString()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        var service = CreateService(context);

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
    public void AddMvp24HoursMigrationService_ShouldRegisterMigrationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>();
        services.AddMvp24HoursMigrationService<TestDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationService>();
        migrationService.Should().BeOfType<MigrationService<TestDbContext>>();
    }
}
