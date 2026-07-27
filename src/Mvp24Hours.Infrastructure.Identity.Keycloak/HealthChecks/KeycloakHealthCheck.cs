using Microsoft.Extensions.Diagnostics.HealthChecks;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;

/// <summary>
/// Verifies that the Keycloak OpenID Connect discovery endpoint is available.
/// </summary>
public sealed class KeycloakHealthCheck(
    IKeycloakDiscoveryService discoveryService) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await discoveryService.GetConfigurationAsync(cancellationToken);
            return HealthCheckResult.Healthy("Keycloak discovery is available.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or InvalidOperationException
            or TaskCanceledException)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Keycloak discovery is unavailable.",
                exception);
        }
    }
}
