using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authentication;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakClaimsTransformationTests
{
    [Fact]
    public async Task TransformAsync_ShouldFlattenRealmClientRolesAndGroups()
    {
        ClaimsIdentity identity = new(
        [
            new Claim(KeycloakClaimTypes.RealmAccess, "{\"roles\":[\"admin\",\"user\"]}"),
            new Claim(
                KeycloakClaimTypes.ResourceAccess,
                "{\"api\":{\"roles\":[\"orders-read\"]},\"other\":{\"roles\":[\"ignored\"]}}"),
            new Claim(KeycloakClaimTypes.Groups, "[\"engineering\",\"support\"]")
        ],
        "test");
        ClaimsPrincipal principal = new(identity);
        KeycloakRolesClaimsTransformation transformation = new(
            Options.Create(new KeycloakAuthorizationOptions
            {
                ResourceServerClientId = "api",
                RealmRoleClaimType = ClaimTypes.Role
            }),
            NullLogger<KeycloakRolesClaimsTransformation>.Instance);

        ClaimsPrincipal result = await transformation.TransformAsync(principal);

        result.IsInRole("admin").Should().BeTrue();
        result.IsInRole("orders-read").Should().BeTrue();
        result.IsInRole("ignored").Should().BeFalse();
        result.FindAll(KeycloakClaimTypes.Groups)
            .Select(claim => claim.Value)
            .Should()
            .Contain(["engineering", "support"]);
        principal.IsInRole("admin").Should().BeFalse(
            "claims transformation must not mutate the source principal");
    }

    [Fact]
    public async Task TransformAsync_WithMalformedJson_ShouldIgnoreMalformedClaims()
    {
        ClaimsPrincipal principal = new(new ClaimsIdentity(
        [
            new Claim(KeycloakClaimTypes.RealmAccess, "{bad-json"),
            new Claim(KeycloakClaimTypes.Groups, "engineering")
        ],
        "test"));
        KeycloakRolesClaimsTransformation transformation = new(
            Options.Create(new KeycloakAuthorizationOptions()),
            NullLogger<KeycloakRolesClaimsTransformation>.Instance);

        ClaimsPrincipal result = await transformation.TransformAsync(principal);

        result.Claims.Should().ContainSingle(claim =>
            claim.Type == KeycloakClaimTypes.Groups
            && claim.Value == "engineering");
        result.Claims.Should().NotContain(claim => claim.Type == ClaimTypes.Role);
    }
}
