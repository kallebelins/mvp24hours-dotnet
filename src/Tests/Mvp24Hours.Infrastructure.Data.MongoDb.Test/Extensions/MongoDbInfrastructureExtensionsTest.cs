using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure;
using Mvp24Hours.Infrastructure.Data.MongoDb.Infrastructure.Migrations;
using Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Indexes;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbInfrastructureExtensionsTest
{
    [Fact]
    public void AddMongoDbIndexVerification_WithoutConfigure_ShouldRegisterHostedServiceAndIndexManager()
    {
        var services = new ServiceCollection();

        services.AddMongoDbIndexVerification();

        services.Should().Contain(d => d.ServiceType == typeof(IMongoDbIndexManager));
        services.Should().Contain(d => d.ImplementationType == typeof(MongoDbIndexVerificationService));
    }

    [Fact]
    public void AddMongoDbIndexVerification_WithConfigure_ShouldApplyOptions()
    {
        var services = new ServiceCollection();
        System.Reflection.Assembly assembly = typeof(MongoDbInfrastructureExtensionsTest).Assembly;

        services.AddMongoDbIndexVerification(options =>
        {
            options.Enabled = true;
            options.AssembliesToScan = [assembly];
            options.CreateMissingIndexes = true;
            options.FailOnMissingIndexes = true;
            options.FailOnVerificationError = true;
            options.StartupDelaySeconds = 5;
        });

        services.Should().Contain(d => d.ImplementationType == typeof(MongoDbIndexVerificationService));
    }

    [Fact]
    public void AddMongoDbMigrations_WithoutAutoMigrate_ShouldRegisterRunnerOnly()
    {
        var services = new ServiceCollection();

        services.AddMongoDbMigrations();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IMongoDbMigrationRunner) &&
            d.ImplementationType == typeof(MongoDbMigrationRunner));
        services.Should().NotContain(d => d.ImplementationType == typeof(MongoDbMigrationHostedService));
    }

    [Fact]
    public void AddMongoDbMigrations_WithAutoMigrate_ShouldRegisterHostedService()
    {
        var services = new ServiceCollection();

        services.AddMongoDbMigrations(options =>
        {
            options.AutoMigrateOnStartup = true;
            options.AppliedBy = "UnitTest";
            options.FailOnMigrationError = true;
            options.StartupDelaySeconds = 2;
        });

        services.Should().Contain(d => d.ImplementationType == typeof(MongoDbMigrationHostedService));
    }

    [Fact]
    public void AddMongoDbInfrastructure_WithBothConfigurations_ShouldRegisterAllServices()
    {
        var services = new ServiceCollection();
        System.Reflection.Assembly assembly = typeof(MongoDbInfrastructureExtensionsTest).Assembly;

        services.AddMongoDbInfrastructure(
            indexOpts =>
            {
                indexOpts.AssembliesToScan = [assembly];
                indexOpts.CreateMissingIndexes = true;
            },
            migrationOpts =>
            {
                migrationOpts.MigrationAssemblies = [assembly];
                migrationOpts.AutoMigrateOnStartup = true;
            });

        services.Should().Contain(d => d.ServiceType == typeof(IMongoDbIndexManager));
        services.Should().Contain(d => d.ImplementationType == typeof(MongoDbIndexVerificationService));
        services.Should().Contain(d => d.ImplementationType == typeof(MongoDbMigrationRunner));
        services.Should().Contain(d => d.ImplementationType == typeof(MongoDbMigrationHostedService));
    }

    [Fact]
    public void AddMongoDbInfrastructure_WithNullConfigurations_ShouldRegisterIndexManagerOnly()
    {
        var services = new ServiceCollection();

        services.AddMongoDbInfrastructure();

        services.Should().ContainSingle(d => d.ServiceType == typeof(IMongoDbIndexManager));
        services.Should().NotContain(d => d.ImplementationType == typeof(MongoDbIndexVerificationService));
        services.Should().NotContain(d => d.ImplementationType == typeof(MongoDbMigrationRunner));
    }

    [Fact]
    public void AddMongoDbIndexManager_ShouldRegisterSingletonIndexManager()
    {
        var services = new ServiceCollection();

        MongoDbInfrastructureExtensions.AddMongoDbIndexManager(services);
        MongoDbInfrastructureExtensions.AddMongoDbIndexManager(services);

        services.Count(d => d.ServiceType == typeof(IMongoDbIndexManager)).Should().Be(1);

        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<IMongoDbIndexManager>().Should().BeOfType<MongoDbIndexManager>();
    }
}
