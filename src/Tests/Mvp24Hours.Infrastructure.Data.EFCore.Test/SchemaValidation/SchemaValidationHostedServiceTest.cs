using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Infrastructure.Data.EFCore.SchemaValidation;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.SchemaValidation;

[Trait("Category", "Unit")]
public class SchemaValidationHostedServiceTest
{
    [Fact]
    public async Task StartAsync_WhenValidationDisabled_ShouldReturnImmediately()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ISchemaValidator>());

        using ServiceProvider provider = services.BuildServiceProvider();
        var hostedService = new SchemaValidationHostedService<TestDbContext>(
            provider,
            Options.Create(new SchemaValidationOptions { ValidateOnStartup = false }),
            provider.GetRequiredService<ILogger<SchemaValidationHostedService<TestDbContext>>>());

        await hostedService.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_WithValidValidator_ShouldCompleteWithoutThrowing()
    {
        var validator = new Mock<ISchemaValidator>();
        validator.Setup(x => x.GetModelSummary()).Returns(new ModelSummary
        {
            ContextType = nameof(TestDbContext),
            EntityCount = 1,
            TableCount = 1,
            AppliedMigrationCount = 0
        });
        validator.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SchemaValidationResult
            {
                IsValid = true,
                Duration = TimeSpan.Zero
            });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(validator.Object);

        using ServiceProvider provider = services.BuildServiceProvider();
        var options = SchemaValidationOptions.Production();
        options.ValidateOnStartup = true;
        options.ThrowOnValidationFailure = false;

        var hostedService = new SchemaValidationHostedService<TestDbContext>(
            provider,
            Options.Create(options),
            provider.GetRequiredService<ILogger<SchemaValidationHostedService<TestDbContext>>>());

        Func<Task> act = () => hostedService.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_ShouldComplete()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Mock.Of<ISchemaValidator>());

        using ServiceProvider provider = services.BuildServiceProvider();
        var hostedService = new SchemaValidationHostedService<TestDbContext>(
            provider,
            Options.Create(new SchemaValidationOptions()),
            provider.GetRequiredService<ILogger<SchemaValidationHostedService<TestDbContext>>>());

        await hostedService.StopAsync(CancellationToken.None);
    }
}
