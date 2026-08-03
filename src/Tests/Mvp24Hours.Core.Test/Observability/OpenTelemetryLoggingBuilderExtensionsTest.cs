using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Observability;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class OpenTelemetryLoggingBuilderExtensionsTest
{
    [Fact]
    public void AddMvp24HoursOpenTelemetryLogging_WithConfigure_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursOpenTelemetryLogging(options =>
        {
            options.ServiceName = "UnitTestService";
            options.ServiceVersion = "2.0.0";
            options.Environment = "Test";
            options.EnableTraceCorrelation = false;
            options.MinimumLevel = LogLevel.Warning;
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        OpenTelemetryLoggingOptions options = provider.GetRequiredService<OpenTelemetryLoggingOptions>();

        options.ServiceName.Should().Be("UnitTestService");
        options.ServiceVersion.Should().Be("2.0.0");
        options.Environment.Should().Be("Test");
        options.EnableTraceCorrelation.Should().BeFalse();
        options.MinimumLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void AddMvp24HoursOpenTelemetryLogging_WithConfiguration_ShouldBindSection()
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mvp24Hours:OpenTelemetry:Logging:ServiceName"] = "ConfigService",
                ["Mvp24Hours:OpenTelemetry:Logging:ServiceVersion"] = "3.1.0",
                ["Mvp24Hours:OpenTelemetry:Logging:EnableOtlpExporter"] = "true",
                ["Mvp24Hours:OpenTelemetry:Logging:OtlpEndpoint"] = "http://collector:4317"
            })
            .Build();

        services.AddMvp24HoursOpenTelemetryLogging(config);

        using ServiceProvider provider = services.BuildServiceProvider();
        OpenTelemetryLoggingOptions options = provider.GetRequiredService<OpenTelemetryLoggingOptions>();

        options.ServiceName.Should().Be("ConfigService");
        options.ServiceVersion.Should().Be("3.1.0");
        options.EnableOtlpExporter.Should().BeTrue();
        options.OtlpEndpoint.Should().Be("http://collector:4317");
        options.Configuration.Should().BeSameAs(config);
    }

    [Fact]
    public void AddMvp24HoursOpenTelemetryConfig_ShouldConfigureLoggingBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder =>
            builder.AddMvp24HoursOpenTelemetryConfig("BuilderService", options =>
            {
                options.MinimumLevel = LogLevel.Error;
                options.EnableTraceCorrelation = false;
            }));

        using ServiceProvider provider = services.BuildServiceProvider();
        ILoggerFactory factory = provider.GetRequiredService<ILoggerFactory>();

        factory.CreateLogger("test").Should().NotBeNull();
    }

    [Fact]
    public void ConfigureMvp24HoursLogLevels_ShouldApplyNamespaceFiltering()
    {
        var services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Mvp24Hours"] = "Debug"
            })
            .Build();

        services.AddLogging(builder => builder.ConfigureMvp24HoursLogLevels(config));

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
    }

    [Fact]
    public void ApplyMvp24HoursDevelopmentDefaults_ShouldConfigureLoggingBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ApplyMvp24HoursDevelopmentDefaults());

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
    }

    [Fact]
    public void ApplyMvp24HoursProductionDefaults_ShouldConfigureLoggingBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ApplyMvp24HoursProductionDefaults());

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILoggerFactory>().Should().NotBeNull();
    }

    [Fact]
    public void OtlpLogRecordAttributes_GetResourceAttributes_ShouldReturnServiceAttributes()
    {
        Dictionary<string, object> attributes =
            OtlpLogRecordAttributes.GetResourceAttributes("svc", "1.0", "Production");

        attributes.Should().NotBeEmpty();
        attributes.Values.Should().Contain("svc");
    }

    [Fact]
    public void OtlpLogRecordAttributes_GetRecommendedConfig_ShouldReturnConfig()
    {
        OpenTelemetryLoggingConfig config = OtlpLogRecordAttributes.GetRecommendedConfig();

        config.Should().NotBeNull();
        config.IncludeFormattedMessage.Should().BeTrue();
    }
}
