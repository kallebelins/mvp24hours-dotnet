namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

/// <summary>
/// Root configuration for Keycloak authentication and OIDC endpoints.
/// Default configuration section: <c>Keycloak</c>.
/// </summary>
public class KeycloakOptions
{
    /// <summary>
    /// Default configuration section name.
    /// </summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// Realm base URL, e.g. <c>https://keycloak.host/realms/myrealm</c>.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Keycloak realm name.
    /// </summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OIDC client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confidential-client secret used for token operations.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the expected access-token audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets whether discovery metadata must use HTTPS.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the token issuer is validated.
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the token audience is validated.
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Gets or sets the allowed clock difference during token validation.
    /// </summary>
    public TimeSpan TokenClockSkew { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Duration for which the OpenID Connect discovery document is cached.
    /// </summary>
    public TimeSpan DiscoveryCacheTtl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Optional override for OpenID Connect discovery metadata URL.
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Validates required settings and returns a list of error messages (empty when valid).
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(Authority))
        {
            errors.Add($"{nameof(Authority)} is required.");
        }
        else if (!Uri.TryCreate(Authority, UriKind.Absolute, out Uri? authorityUri)
            || (authorityUri.Scheme != Uri.UriSchemeHttps
                && authorityUri.Scheme != Uri.UriSchemeHttp))
        {
            errors.Add($"{nameof(Authority)} must be an absolute HTTP or HTTPS URL.");
        }
        else if (RequireHttpsMetadata && authorityUri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add($"{nameof(Authority)} must use HTTPS when {nameof(RequireHttpsMetadata)} is true.");
        }

        if (string.IsNullOrWhiteSpace(Realm))
        {
            errors.Add($"{nameof(Realm)} is required.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            errors.Add($"{nameof(ClientId)} is required.");
        }

        if (TokenClockSkew < TimeSpan.Zero)
        {
            errors.Add($"{nameof(TokenClockSkew)} cannot be negative.");
        }

        if (DiscoveryCacheTtl <= TimeSpan.Zero)
        {
            errors.Add($"{nameof(DiscoveryCacheTtl)} must be greater than zero.");
        }

        if (!string.IsNullOrWhiteSpace(MetadataAddress)
            && !Uri.TryCreate(MetadataAddress, UriKind.Absolute, out _))
        {
            errors.Add($"{nameof(MetadataAddress)} must be an absolute URL when provided.");
        }

        if (ValidateAudience && string.IsNullOrWhiteSpace(Audience))
        {
            errors.Add($"{nameof(Audience)} is required when {nameof(ValidateAudience)} is true.");
        }

        return errors;
    }
}
