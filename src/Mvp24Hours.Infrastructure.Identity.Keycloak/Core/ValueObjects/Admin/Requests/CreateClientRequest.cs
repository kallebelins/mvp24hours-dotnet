namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Admin.Requests;

/// <summary>
/// Request to create a client via the Keycloak Admin API.
/// </summary>
public sealed record CreateClientRequest
{
    public string ClientId { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Description { get; init; }

    public bool Enabled { get; init; } = true;

    public bool PublicClient { get; init; }

    public bool ServiceAccountsEnabled { get; init; }

    public bool DirectAccessGrantsEnabled { get; init; }

    public bool StandardFlowEnabled { get; init; } = true;

    public bool AuthorizationServicesEnabled { get; init; }

    public string Protocol { get; init; } = "openid-connect";

    public string? Secret { get; init; }

    public string? RootUrl { get; init; }

    public string? BaseUrl { get; init; }

    public IReadOnlyList<string>? RedirectUris { get; init; }

    public IReadOnlyList<string>? WebOrigins { get; init; }

    public Dictionary<string, string>? Attributes { get; init; }

    public bool IsValid => Validate().Count == 0;

    public IReadOnlyList<string> Validate()
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            errors.Add($"{nameof(ClientId)} is required.");
        }

        if (string.IsNullOrWhiteSpace(Protocol))
        {
            errors.Add($"{nameof(Protocol)} is required.");
        }

        return errors;
    }
}
