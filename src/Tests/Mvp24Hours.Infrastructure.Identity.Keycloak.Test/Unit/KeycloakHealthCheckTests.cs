using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;
using Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenDiscoverySucceeds_ShouldReturnHealthy()
    {
        Mock<IKeycloakDiscoveryService> discovery = new();
        discovery.Setup(service => service.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectConfigurationDocument());
        KeycloakHealthCheck healthCheck = new(discovery.Object);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(
                "keycloak",
                healthCheck,
                HealthStatus.Unhealthy,
                [])
        };

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("available");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenDiscoveryFails_ShouldReturnConfiguredFailureStatus()
    {
        Mock<IKeycloakDiscoveryService> discovery = new();
        discovery.Setup(service => service.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("offline"));
        KeycloakHealthCheck healthCheck = new(discovery.Object);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(
                "keycloak",
                healthCheck,
                HealthStatus.Degraded,
                [])
        };

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("unavailable");
        result.Exception.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public void AddKeycloakHealthCheck_ShouldRegisterNamedCheck()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IKeycloakDiscoveryService>(Mock.Of<IKeycloakDiscoveryService>());

        IHealthChecksBuilder builder = services.AddHealthChecks()
            .AddKeycloakHealthCheck("keycloak-discovery", HealthStatus.Unhealthy, ["identity"]);

        builder.Should().NotBeNull();
        ServiceProvider provider = services.BuildServiceProvider();
        HealthCheckService healthCheckService = provider.GetRequiredService<HealthCheckService>();

        healthCheckService.Should().NotBeNull();
    }
}
