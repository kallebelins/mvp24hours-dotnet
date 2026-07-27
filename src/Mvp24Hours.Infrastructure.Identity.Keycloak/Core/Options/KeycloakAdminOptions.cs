namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

/// <summary>
/// Configuration for Keycloak Admin REST API clients.
/// Default configuration section: <c>Keycloak:Admin</c>.
/// </summary>
public class KeycloakAdminOptions
{
    public const string SectionName = "Keycloak:Admin";

    /// <summary>
    /// Admin API base URL, e.g. <c>https://keycloak.host/admin/realms/{realm}</c>.
    /// </summary>
    public string AdminBaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;

    /// <summary>
    /// Confidential client with realm-management (or equivalent) permissions.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    public string? ClientSecret { get; set; }

    public bool ServiceAccountEnabled { get; set; } = true;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Validates required settings and returns a list of error messages (empty when valid).
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(AdminBaseUrl))
        {
            errors.Add($"{nameof(AdminBaseUrl)} is required.");
        }
        else if (!Uri.TryCreate(AdminBaseUrl, UriKind.Absolute, out Uri? baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            errors.Add($"{nameof(AdminBaseUrl)} must be an absolute HTTP or HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(Realm))
        {
            errors.Add($"{nameof(Realm)} is required.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            errors.Add($"{nameof(ClientId)} is required.");
        }

        if (ServiceAccountEnabled && string.IsNullOrWhiteSpace(ClientSecret))
        {
            errors.Add($"{nameof(ClientSecret)} is required when {nameof(ServiceAccountEnabled)} is true.");
        }

        if (Timeout <= TimeSpan.Zero)
        {
            errors.Add($"{nameof(Timeout)} must be greater than zero.");
        }

        if (RetryCount < 0)
        {
            errors.Add($"{nameof(RetryCount)} cannot be negative.");
        }

        return errors;
    }
}
