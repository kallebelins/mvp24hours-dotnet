namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

/// <summary>
/// Parameters for an OAuth 2.0 client credentials token request against Keycloak.
/// </summary>
public class ClientCredentialsTokenRequest
{
    /// <summary>
    /// Absolute token endpoint URL, or relative path when the HttpClient has a BaseAddress.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string? Scope { get; set; }
}
