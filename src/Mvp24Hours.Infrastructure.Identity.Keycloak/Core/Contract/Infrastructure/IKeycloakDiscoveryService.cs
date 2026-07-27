using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;

/// <summary>
/// Resolves endpoints from the Keycloak OpenID Connect discovery document.
/// </summary>
public interface IKeycloakDiscoveryService
{
    Task<OpenIdConnectConfigurationDocument> GetConfigurationAsync(
        CancellationToken cancellationToken = default);

    Task<string> GetTokenEndpointAsync(CancellationToken cancellationToken = default);

    Task<string> GetIntrospectionEndpointAsync(CancellationToken cancellationToken = default);

    Task<string> GetRevocationEndpointAsync(CancellationToken cancellationToken = default);

    Task<string> GetJwksUriAsync(CancellationToken cancellationToken = default);
}
