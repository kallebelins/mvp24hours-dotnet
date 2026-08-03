using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.SchemaValidation;

[Trait("Category", "Unit")]
public class SchemaValidatorTest
{
    [Fact]
    public async Task ValidateConnectivityAsync_WithCreatedInMemoryDatabase_ShouldReturnTrue()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context);

        bool canConnect = await validator.ValidateConnectivityAsync();

        canConnect.Should().BeTrue();
    }

    [Fact]
    public void GetModelSummary_OnInMemory_ShouldThrowForAppliedMigrationsLookup()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context);

        Action act = () => validator.GetModelSummary();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Relational-specific*");
    }

    [Fact]
    public async Task ValidateAsync_WithTableChecksDisabled_ShouldReturnValidOnInMemory()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = false;
            options.ValidateColumns = false;
            options.CacheValidationResults = false;
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.IsValid.Should().BeTrue();
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithTableChecksEnabled_OnInMemory_ShouldNotThrow()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = true;
            options.ValidateColumns = false;
            options.CacheValidationResults = false;
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithCaching_ShouldReturnCachedResult()
    {
        using TestDbContext context = EfCoreTestHelpers.CreateContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = false;
            options.ValidateColumns = false;
            options.CacheValidationResults = true;
            options.CacheDuration = TimeSpan.FromMinutes(5);
        });

        SchemaValidationResult first = await validator.ValidateAsync();
        SchemaValidationResult second = await validator.ValidateAsync();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task ValidateAsync_WithSqliteAndTableValidation_ShouldValidateExistingTables()
    {
        await using TestDbContext context = CreateSqliteContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = true;
            options.ValidateColumns = false;
            options.CacheValidationResults = false;
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.Should().NotBeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetModelSummary_WithSqlite_ShouldReturnSummary()
    {
        using TestDbContext context = CreateSqliteContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context);

        ModelSummary summary = validator.GetModelSummary();

        summary.ContextType.Should().Be(nameof(TestDbContext));
        summary.EntityCount.Should().BeGreaterThan(0);
        summary.TableCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ValidateConnectivityAsync_WithClosedSqliteConnection_ShouldReturnTrue()
    {
        await using TestDbContext context = CreateSqliteContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context);

        bool canConnect = await validator.ValidateConnectivityAsync();

        canConnect.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithColumnValidation_ShouldCompleteOnSqlite()
    {
        await using TestDbContext context = CreateSqliteContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = true;
            options.ValidateColumns = true;
            options.CacheValidationResults = false;
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.Should().NotBeNull();
        result.Warnings.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithExcludedTables_ShouldSkipExcluded()
    {
        await using TestDbContext context = CreateSqliteContext();
        string tableName = new SchemaValidator<TestDbContext>(
            context,
            Options.Create(new SchemaValidationOptions()),
            new LoggerFactory().CreateLogger<SchemaValidator<TestDbContext>>())
            .GetModelSummary().Tables.First();

        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = true;
            options.ValidateColumns = false;
            options.CacheValidationResults = false;
            options.ExcludedTables.Add(tableName);
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithFailedConnectivity_ShouldReturnInvalidResult()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:invalid:")
            .Options;
        await using var context = new TestDbContext(options);
        SchemaValidator<TestDbContext> validator = CreateValidator(context, o =>
        {
            o.CheckPendingMigrations = false;
            o.ValidateTables = false;
            o.ValidateColumns = false;
            o.CacheValidationResults = false;
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Type == IssueType.ConnectionFailed);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidIssues_ShouldMarkResultInvalid()
    {
        await using TestDbContext context = CreateSqliteContext();
        SchemaValidator<TestDbContext> validator = CreateValidator(context, options =>
        {
            options.CheckPendingMigrations = false;
            options.ValidateTables = true;
            options.ValidateColumns = false;
            options.CacheValidationResults = false;
        });

        SchemaValidationResult result = await validator.ValidateAsync();

        result.Issues.Should().NotBeNull();
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    private static TestDbContext CreateSqliteContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite($"Data Source=file:schema_{Guid.NewGuid():N}?mode=memory&cache=shared")
            .Options;
        var context = new TestDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private static SchemaValidator<TestDbContext> CreateValidator(
        TestDbContext context,
        Action<SchemaValidationOptions>? configure = null)
    {
        var options = new SchemaValidationOptions();
        configure?.Invoke(options);
        ILogger<SchemaValidator<TestDbContext>> logger = new LoggerFactory().CreateLogger<SchemaValidator<TestDbContext>>();
        return new SchemaValidator<TestDbContext>(context, Options.Create(options), logger);
    }
}
