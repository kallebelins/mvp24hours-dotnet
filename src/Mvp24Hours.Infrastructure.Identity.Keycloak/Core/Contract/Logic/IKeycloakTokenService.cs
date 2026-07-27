using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Logic;

/// <summary>
/// First-party OIDC/OAuth token operations against Keycloak endpoints.
/// </summary>
public interface IKeycloakTokenService
{
    /// <summary>
    /// Requests an access token using the configured client credentials.
    /// </summary>
    Task<IBusinessResult<AccessTokenResponse>> GetClientCredentialsTokenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges a refresh token for a new token response.
    /// </summary>
    Task<IBusinessResult<AccessTokenResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Introspects a token at the discovered Keycloak endpoint.
    /// </summary>
    Task<IBusinessResult<TokenIntrospectionResponse>> IntrospectTokenAsync(
        string token,
        string? tokenTypeHint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes an access or refresh token.
    /// </summary>
    Task<IBusinessResult<bool>> RevokeTokenAsync(
        string token,
        string? tokenTypeHint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resource Owner Password Credentials grant. Intended for automated tests only.
    /// </summary>
    Task<IBusinessResult<AccessTokenResponse>> GetPasswordTokenAsync(
        string username,
        string password,
        string? scope = null,
        CancellationToken cancellationToken = default);
}
