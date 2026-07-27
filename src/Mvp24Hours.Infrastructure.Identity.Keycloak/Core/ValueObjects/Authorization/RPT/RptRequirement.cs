using Microsoft.AspNetCore.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.RPT;

/// <summary>
/// Represents a resource and scope required in a Requesting Party Token.
/// </summary>
public class RptRequirement(string resource, string scope) : IAuthorizationRequirement
{
    public string Resource { get; } = resource;

    public string Scope { get; } = scope;
}
