using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Infrastructure.Data.EFCore;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class RepositoryWithLoggerCoverageTest
{
    [Fact]
    public void Repository_WithLogger_ShouldExecuteQueryMethods()
    {
        using TestDbContext context = CreateContext();
        SeedEntities(context, 3);
        var logger = new Mock<ILogger<Repository<TestEntity>>>();
        var repository = new Repository<TestEntity>(
            context,
            Options.Create(new EFCoreRepositoryOptions()),
            logger.Object);

        repository.ListAny().Should().BeTrue();
        repository.ListCount().Should().Be(3);
        repository.List().Should().HaveCount(3);
        repository.GetByAny(e => e.Active).Should().BeTrue();
        repository.GetByCount(e => e.Active).Should().BeGreaterThan(0);
        repository.GetBy(e => e.Active).Should().NotBeEmpty();

        logger.Verify(
            l => l.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Repository", StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task MigrationService_RollbackToAsync_WithUnappliedTarget_ShouldReturnFailed()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = new(
            context,
            Options.Create(new MigrationOptions()),
            NullLogger<MigrationService<TestDbContext>>.Instance);

        MigrationResult result = await service.RollbackToAsync("NonAppliedMigration");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("has not been applied");
    }

    [Fact]
    public async Task MigrationService_ValidateSchemaAsync_WithExceptionInModel_ShouldReturnInvalid()
    {
        await using InvalidSchemaDbContext context = CreateInvalidSchemaContext();
        MigrationService<InvalidSchemaDbContext> service = new(
            context,
            Options.Create(new MigrationOptions()),
            NullLogger<MigrationService<InvalidSchemaDbContext>>.Instance);

        SchemaValidationResult result = await service.ValidateSchemaAsync();

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task MigrationService_GetMigrationScriptAsync_WithRange_ShouldReturnNonNullScript()
    {
        await using TestDbContext context = CreateSqliteContext();
        MigrationService<TestDbContext> service = new(
            context,
            Options.Create(new MigrationOptions()),
            NullLogger<MigrationService<TestDbContext>>.Instance);

        string script = await service.GetMigrationScriptAsync(fromMigration: null, toMigration: null);

        script.Should().NotBeNull();
    }

    private static TestDbContext CreateContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"RepoLogger_{Guid.NewGuid():N}")
            .Options;
        return new TestDbContext(options);
    }

    private static TestDbContext CreateSqliteContext()
    {
        string connectionString = $"Data Source=file:repo_logger_{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAlive = new SqliteConnection(connectionString);
        keepAlive.Open();
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(keepAlive).Options;
        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static InvalidSchemaDbContext CreateInvalidSchemaContext()
    {
        DbContextOptions<InvalidSchemaDbContext> options = new DbContextOptionsBuilder<InvalidSchemaDbContext>()
            .UseInMemoryDatabase($"InvalidSchemaLogger_{Guid.NewGuid():N}")
            .Options;
        var context = new InvalidSchemaDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void SeedEntities(TestDbContext context, int count)
    {
        for (int i = 0; i < count; i++)
        {
            context.Entities.Add(new TestEntity
            {
                Name = $"Entity-{i}",
                Active = i % 2 == 0,
                Score = i * 10
            });
        }
        context.SaveChanges();
    }
}
