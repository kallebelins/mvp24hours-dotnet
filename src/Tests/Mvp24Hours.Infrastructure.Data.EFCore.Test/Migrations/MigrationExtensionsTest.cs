using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationExtensionsTest
{
    [Fact]
    public void AddMvp24HoursAutoMigration_ShouldRegisterHostedServiceAndMigrationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>();
        services.AddMvp24HoursAutoMigration<TestDbContext>(MigrationOptions.LogOnly());

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(MigrationHostedService<TestDbContext>));
        services.Should().Contain(d => d.ServiceType == typeof(IMigrationService));
    }

    [Fact]
    public void AddMvp24HoursDataSeeder_ShouldRegisterSeeder()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursDataSeeder<TestMigrationSeeder>();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IDataSeeder) &&
            d.ImplementationType == typeof(TestMigrationSeeder));
    }

    [Fact]
    public void AddMvp24HoursDevMigration_ShouldUseDevelopmentOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>();
        services.AddMvp24HoursDevMigration<TestDbContext>();

        using ServiceProvider provider = services.BuildServiceProvider();
        MigrationOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MigrationOptions>>().Value;

        options.AutoMigrateOnStartup.Should().BeTrue();
        options.EnableDataSeeding.Should().BeTrue();
    }

    private sealed class TestMigrationSeeder : IDataSeeder
    {
        public Task SeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
