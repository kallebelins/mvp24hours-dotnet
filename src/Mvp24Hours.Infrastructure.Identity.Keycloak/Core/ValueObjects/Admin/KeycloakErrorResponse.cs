using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin;

/// <summary>
/// Error payload returned by Keycloak Admin or OIDC endpoints.
/// </summary>
public class KeycloakErrorResponse
{
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Preferred human-readable message from available error fields.
    /// </summary>
    [JsonIgnore]
    public string Message =>
        !string.IsNullOrWhiteSpace(ErrorMessage)
            ? ErrorMessage
            : !string.IsNullOrWhiteSpace(ErrorDescription)
                ? ErrorDescription
                : Error ?? string.Empty;
}
