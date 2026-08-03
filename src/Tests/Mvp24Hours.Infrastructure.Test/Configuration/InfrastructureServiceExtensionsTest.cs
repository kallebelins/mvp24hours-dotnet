using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.BackgroundJobs.Extensions;
using Mvp24Hours.Infrastructure.Configuration;
using Mvp24Hours.Infrastructure.DistributedLocking.Extensions;
using Mvp24Hours.Infrastructure.Email.Contract;
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Mvp24Hours.Infrastructure.Resilience.Options;
using Mvp24Hours.Infrastructure.Security.Contract;
using Mvp24Hours.Infrastructure.Security.Options;
using Mvp24Hours.Infrastructure.Sms.Contract;

namespace Mvp24Hours.Infrastructure.Test.Configuration;

[Trait("Category", "Unit")]
public class InfrastructureServiceExtensionsTest
{
    [Fact]
    public void AddMvpInfrastructure_WithNullServices_ShouldThrow()
    {
        Action act = () => InfrastructureServiceExtensions.AddMvpInfrastructure(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddMvpInfrastructure_WithDefaults_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpInfrastructure(configure: builder => builder.Options.ValidateOnStart = false);

        ServiceProvider provider = services.BuildServiceProvider();
        InfrastructureOptions options = provider.GetRequiredService<IOptions<InfrastructureOptions>>().Value;

        options.ValidateOnStart.Should().BeFalse();
        options.EnableLazyInitialization.Should().BeTrue();
    }

    [Fact]
    public void AddMvpInfrastructure_WithConfigurationSection_ShouldBindOptions()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Infrastructure:EnableLazyInitialization"] = "false",
                ["Infrastructure:ValidateOnStart"] = "false",
                ["Infrastructure:Email:DefaultFrom"] = "noreply@infra.test"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpInfrastructure(configuration, builder => builder.Options.ValidateOnStart = false);

        InfrastructureOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<InfrastructureOptions>>().Value;

        options.Email.Should().NotBeNull();
        options.Email!.DefaultFrom.Should().Be("noreply@infra.test");
    }

    [Fact]
    public void AddMvpInfrastructure_WithFluentFlags_ShouldApplyBuilderOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpInfrastructure(configure: builder =>
        {
            builder.Options.ValidateOnStart = false;
            builder.Options.EnableLazyInitialization = false;
        });

        InfrastructureOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<InfrastructureOptions>>().Value;

        options.EnableLazyInitialization.Should().BeFalse();
        options.ValidateOnStart.Should().BeFalse();
    }

    [Fact]
    public void AddMvpInfrastructure_WithFluentSubsystems_ShouldRegisterServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpInfrastructure(configure: builder =>
        {
            builder.Options.ValidateOnStart = false;
            builder.ConfigureEmail(options => options.DefaultFrom = "email@test.com");
            builder.ConfigureSms(options => options.DefaultFrom = "+15550001111");
            builder.ConfigureFileStorage(options =>
            {
                options.BasePath = "C:\\storage";
                options.MaxFileSize = 1024;
                options.AllowedExtensions = [".txt"];
            });
            builder.ConfigureObservability(options =>
            {
                options.EnableMetrics = true;
                options.EnableTracing = true;
            });
            builder.ConfigureResilience(options =>
            {
                options.Retry = new RetryOptions { MaxRetries = 5, InitialDelay = TimeSpan.FromMilliseconds(50) };
                options.CircuitBreaker = new CircuitBreakerOptions { FailureThreshold = 3, BreakDuration = TimeSpan.FromSeconds(10) };
            });
            builder.ConfigureSecurity(options =>
            {
                options.SecretProvider = new SecretProviderOptions();
                options.EnvironmentVariable = new EnvironmentVariableOptions
                {
                    VariableNamePrefix = "TEST_SECRET_",
                    CaseSensitive = false
                };
            });
            builder.ConfigureDistributedLocking(locking => locking.AddInMemoryProvider());
            builder.ConfigureBackgroundJobs(jobs => jobs.AddInMemoryProvider());
        });

        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IEmailService>().Should().NotBeNull();
        provider.GetRequiredService<ISmsService>().Should().NotBeNull();
        provider.GetRequiredService<IFileStorage>().Should().NotBeNull();
        provider.GetRequiredService<ISecretProvider>().Should().NotBeNull();
        provider.GetRequiredService<IOptions<RetryOptions>>().Value.MaxRetries.Should().Be(5);
        provider.GetRequiredService<IOptions<CircuitBreakerOptions>>().Value.FailureThreshold.Should().Be(3);
    }

    [Fact]
    public void AddMvpInfrastructure_WithObservabilityFromOptions_ShouldRegisterObservability()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvpInfrastructure(configure: builder =>
        {
            builder.Options.ValidateOnStart = false;
            builder.ConfigureObservability(options => options.EnableDetailedLogging = true);
        });

        ServiceProvider provider = services.BuildServiceProvider();
        InfrastructureOptions options = provider.GetRequiredService<IOptions<InfrastructureOptions>>().Value;

        options.Observability.Should().NotBeNull();
        options.Observability!.EnableDetailedLogging.Should().BeTrue();
    }
}
