using Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.SchemaValidation;

[Trait("Category", "Unit")]
public class SchemaValidationOptionsTest
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var options = new SchemaValidationOptions();

        options.ValidateOnStartup.Should().BeFalse();
        options.ThrowOnValidationFailure.Should().BeFalse();
        options.CheckPendingMigrations.Should().BeTrue();
        options.ValidateTables.Should().BeTrue();
        options.ValidateColumns.Should().BeFalse();
        options.ValidateIndexes.Should().BeFalse();
        options.ValidateForeignKeys.Should().BeFalse();
        options.ValidationTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.EnableDetailedLogging.Should().BeTrue();
        options.CacheValidationResults.Should().BeTrue();
        options.CacheDuration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void Development_ShouldEnableStrictValidation()
    {
        SchemaValidationOptions options = SchemaValidationOptions.Development();

        options.ValidateOnStartup.Should().BeTrue();
        options.ThrowOnValidationFailure.Should().BeTrue();
        options.ValidateTables.Should().BeTrue();
        options.ValidateColumns.Should().BeTrue();
    }

    [Fact]
    public void ContinuousIntegration_ShouldDisableCache()
    {
        SchemaValidationOptions options = SchemaValidationOptions.ContinuousIntegration();

        options.CacheValidationResults.Should().BeFalse();
        options.ValidateIndexes.Should().BeTrue();
        options.ValidateForeignKeys.Should().BeTrue();
    }
}
