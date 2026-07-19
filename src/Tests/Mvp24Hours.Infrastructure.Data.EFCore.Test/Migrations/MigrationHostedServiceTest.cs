using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Data.EFCore.Migrations;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Migrations;

[Trait("Category", "Unit")]
public class MigrationHostedServiceTest
{
    [Fact]
    public async Task StartAsync_WithNoPendingMigrations_ShouldCompleteWithoutThrowing()
    {
        var migrationService = new Mock<IMigrationService>();
        migrationService.Setup(x => x.HasPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var services = new ServiceCollection();
        services.AddSingleton(migrationService.Object);
        services.AddLogging();
        services.AddSingleton(Options.Create(new MigrationOptions()));

        using ServiceProvider provider = services.BuildServiceProvider();
        var hostedService = new MigrationHostedService<TestDbContext>(
            provider,
            Options.Create(MigrationOptions.LogOnly()),
            provider.GetRequiredService<ILogger<MigrationHostedService<TestDbContext>>>());

        Func<Task> act = () => hostedService.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldComplete()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMigrationService>(Mock.Of<IMigrationService>());

        using ServiceProvider provider = services.BuildServiceProvider();
        var hostedService = new MigrationHostedService<TestDbContext>(
            provider,
            Options.Create(new MigrationOptions()),
            provider.GetRequiredService<ILogger<MigrationHostedService<TestDbContext>>>());

        await hostedService.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithPendingMigrationsAndAutoMigrate_ShouldInvokeMigrate()
    {
        var migrationService = new Mock<IMigrationService>();
        migrationService.Setup(x => x.HasPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        migrationService.Setup(x => x.GetPendingMigrationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Pending_1"]);
        migrationService.Setup(x => x.MigrateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(MigrationResult.NoMigrationsNeeded());

        var services = new ServiceCollection();
        services.AddSingleton(migrationService.Object);
        services.AddLogging();

        using ServiceProvider provider = services.BuildServiceProvider();
        var options = MigrationOptions.Development();
        options.MaxRetryAttempts = 1;
        options.RetryDelay = TimeSpan.Zero;

        var hostedService = new MigrationHostedService<TestDbContext>(
            provider,
            Options.Create(options),
            provider.GetRequiredService<ILogger<MigrationHostedService<TestDbContext>>>());

        await hostedService.StartAsync(CancellationToken.None);

        migrationService.Verify(x => x.MigrateAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
