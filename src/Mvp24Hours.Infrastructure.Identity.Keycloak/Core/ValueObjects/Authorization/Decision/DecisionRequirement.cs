using Microsoft.AspNetCore.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization.Decision;

/// <summary>
/// Represents a Keycloak UMA decision request.
/// </summary>
public class DecisionRequirement(string resource, string scope) : IAuthorizationRequirement
{
    public string Resource { get; } = resource;

    public string Scope { get; } = scope;
}
