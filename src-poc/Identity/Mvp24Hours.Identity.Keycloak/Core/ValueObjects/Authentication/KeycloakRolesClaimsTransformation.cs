using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mvp24Hours.Identity.Keycloak.Core.ValueObjects.Authentication
{
    /// <summary>
    /// Transforms keycloak roles in the resource_access claim to jwt role claims.
    /// </summary>
    /// <example>
    /// Example of keycloack resource_access claim
    /// "resource_access": {
    ///     "api": {
    ///         "roles": [
    ///             "projectmanager"
    ///         ]
    ///     },
    ///     "account": {
    ///         "roles": [
    ///             "view-profile"
    ///         ]
    ///     }
    /// },
    /// </example>
    /// <seealso cref="IClaimsTransformation" />
    public class KeycloakRolesClaimsTransformation : IClaimsTransformation
    {
        private readonly string _roleClaimType;
        private readonly string _realmScope;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeycloakRolesClaimsTransformation"/> class.
        /// </summary>
        /// <param name="roleClaimType">Type of the role claim.</param>
        /// <param name="realmScope">The audience.</param>
        public KeycloakRolesClaimsTransformation(string roleClaimType, string realmScope)
        {
            _roleClaimType = roleClaimType;
            _realmScope = realmScope;
        }

        /// <summary>
        /// Provides a central transformation point to change the specified principal.
        /// Note: this will be run on each AuthenticateAsync call, so its safer to
        /// return a new ClaimsPrincipal if your transformation is not idempotent.
        /// </summary>
        /// <param name="principal">The <see cref="T:System.Security.Claims.ClaimsPrincipal" /> to transform.</param>
        /// <returns>
        /// The transformed principal.
        /// </returns>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var result = principal.Clone();
            if (result.Identity is not ClaimsIdentity identity)
            {
                return Task.FromResult(result);
            }

            var realmAccessValue = principal.FindFirst(_realmScope)?.Value;
            if (string.IsNullOrWhiteSpace(realmAccessValue))
            {
                return Task.FromResult(result);
            }

            using var realmAccess = JsonDocument.Parse(realmAccessValue);
            realmAccess.RootElement.TryGetProperty("roles", out JsonElement clientRoles);

            if (!clientRoles.Equals(default(JsonElement)))
            {
                foreach (var role in clientRoles.EnumerateArray())
                {
                    var value = role.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        identity.AddClaim(new Claim(_roleClaimType, value));
                    }
                }
            }

            return Task.FromResult(result);
        }
    }
}
