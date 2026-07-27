using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.HealthChecks;

/// <summary>
/// Registration extensions for the Keycloak health check.
/// </summary>
public static class KeycloakHealthCheckExtensions
{
    public static IHealthChecksBuilder AddKeycloakHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "keycloak",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.AddCheck<KeycloakHealthCheck>(
            name,
            failureStatus,
            tags,
            timeout);
    }
}
