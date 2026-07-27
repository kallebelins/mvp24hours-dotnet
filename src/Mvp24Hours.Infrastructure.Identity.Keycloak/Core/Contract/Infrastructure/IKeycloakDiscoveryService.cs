using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;

/// <summary>
/// Resolves endpoints from the Keycloak OpenID Connect discovery document.
/// </summary>
public interface IKeycloakDiscoveryService
{
    /// <summary>
    /// Gets the cached OpenID Connect discovery document.
    /// </summary>
    Task<OpenIdConnectConfigurationDocument> GetConfigurationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the discovered token endpoint.
    /// </summary>
    Task<string> GetTokenEndpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the discovered token introspection endpoint.
    /// </summary>
    Task<string> GetIntrospectionEndpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the discovered token revocation endpoint.
    /// </summary>
    Task<string> GetRevocationEndpointAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the discovered JSON Web Key Set endpoint.
    /// </summary>
    Task<string> GetJwksUriAsync(CancellationToken cancellationToken = default);
}
