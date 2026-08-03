using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Observability;

namespace Mvp24Hours.Core.Test.Observability;

[Trait("Category", "Unit")]
public class OpenTelemetryExporterExtensionsTest
{
    [Fact]
    public void AddMvp24HoursOpenTelemetry_ShouldRegisterExporterOptions()
    {
        var services = new ServiceCollection();
        services.AddMvp24HoursOpenTelemetry(options =>
        {
            options.ServiceName = "TestService";
            options.ServiceVersion = "1.0.0";
            options.Otlp.Enabled = true;
            options.Otlp.Endpoint = "http://localhost:4317";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        OpenTelemetryExporterOptions options = provider.GetRequiredService<OpenTelemetryExporterOptions>();
        options.ServiceName.Should().Be("TestService");
        options.Otlp.Enabled.Should().BeTrue();
        Mvp24HoursMeters.AllMeterNames.Should().HaveCount(9);
    }
}
