using Microsoft.AspNetCore.Authorization;

namespace Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authorization.RPT
{
    /// <summary>
    /// Requesting Party Token (RPT) - User-Managed Access (UMA) protocol with Keycloak
    /// </summary>
    /// <remarks>
    /// Client uses a special acess token containing the permissions to access the Resource Server
    /// </remarks>
    public class RptRequirement : IAuthorizationRequirement
    {
        public string Resource { get; }
        public string Scope { get; }

        public RptRequirement(string resource, string scope)
        {
            Resource = resource;
            Scope = scope;
        }
    }
}