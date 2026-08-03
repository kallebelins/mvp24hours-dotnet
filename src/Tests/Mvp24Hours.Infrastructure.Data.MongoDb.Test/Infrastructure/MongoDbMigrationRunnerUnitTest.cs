using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Infrastructure;

[Trait("Category", "Unit")]
public class MongoDbMigrationRunnerUnitTest
{
    [Fact]
    public void Constructor_WithNullContext_ShouldThrow()
    {
        var options = Options.Create(new MongoDbMigrationOptions());

        Action act = () => new MongoDbMigrationRunner(null!, options);

        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrow()
    {
        Mvp24HoursContext context = new("unit_test_db", "mongodb://localhost:27017");

        Action act = () => new MongoDbMigrationRunner(context, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }
}
