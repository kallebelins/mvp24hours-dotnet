using Microsoft.AspNetCore.Authorization;

namespace Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization.Decision
{
    /// <summary>
    /// Decicion Request - User-Managed Access (UMA) protocol extensions with Keycloak
    /// </summary>
    /// <remarks>
    /// Resource Server makes a special UMA request to Keycloak to check if user have permission
    /// </remarks>
    public class DecisionRequirement : IAuthorizationRequirement
    {
        public string Resource { get; }
        public string Scope { get; }

        public DecisionRequirement(string resource, string scope)
        {
            Resource = resource;
            Scope = scope;
        }
    }

}