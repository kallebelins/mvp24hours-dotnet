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

    private static SchemaValidator<TestDbContext> CreateValidator(
        TestDbContext context,
        Action<SchemaValidationOptions>? configure = null)
    {
        var options = new SchemaValidationOptions();
        configure?.Invoke(options);
        var logger = new LoggerFactory().CreateLogger<SchemaValidator<TestDbContext>>();
        return new SchemaValidator<TestDbContext>(context, Options.Create(options), logger);
    }
}
