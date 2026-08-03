using System.Text.Json;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class UserTokenTests
{
    [Fact]
    public void FromJsonElement_ShouldParseFullPayload()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "sub": "11111111-1111-1111-1111-111111111111",
              "preferred_username": "alice",
              "email": "alice@example.test",
              "email_verified": true,
              "scope": "openid profile",
              "sid": "22222222-2222-2222-2222-222222222222",
              "session_state": "33333333-3333-3333-3333-333333333333",
              "azp": "api",
              "allowed-origins": ["https://app.example"],
              "realm_access": { "roles": ["admin", "user"] },
              "resource_access": {
                "api": { "roles": ["read", "write"] },
                "reports": { "roles": ["view"] }
              },
              "groups": ["engineering"],
              "attributes": {
                "department": ["sales"],
                "level": "senior"
              },
              "iat": 1700000000,
              "exp": 1700003600
            }
            """);

        var token = UserToken.FromJsonElement(document.RootElement);

        token.Id.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        token.PreferredUserName.Should().Be("alice");
        token.EmailVerified.Should().BeTrue();
        token.Scope.Should().Be("openid profile");
        token.SessionId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        token.AuthorizedParty.Should().Be("api");
        token.AllowedOrigins.Should().ContainSingle("https://app.example");
        token.HasRealmRole("ADMIN").Should().BeTrue();
        token.HasClientRole("api", "read").Should().BeTrue();
        token.HasClientRole("reports", "view").Should().BeTrue();
        token.HasGroup("engineering").Should().BeTrue();
        token.Attributes.Should().ContainKey("department");
        token.IssuedAt.Should().NotBeNull();
        token.ExpiresAt.Should().NotBeNull();
        token.ResourceRoles.Should().Contain("read");
    }

    [Fact]
    public void FromJwtPayloadJson_WithEmptyInput_ShouldReturnNull()
    {
        UserToken.FromJwtPayloadJson(null).Should().BeNull();
        UserToken.FromJwtPayloadJson(string.Empty).Should().BeNull();
    }

    [Fact]
    public void RoleAndGroupChecks_WithMissingValues_ShouldReturnFalse()
    {
        UserToken token = new();

        token.HasRealmRole("admin").Should().BeFalse();
        token.HasClientRole("api", "read").Should().BeFalse();
        token.HasGroup("engineering").Should().BeFalse();
        token.HasRealmRole(string.Empty).Should().BeFalse();
    }

    [Fact]
    public void FromJsonElement_ShouldSupportLegacyGroupClaimName()
    {
        using var document = JsonDocument.Parse(
            """{"sub":"11111111-1111-1111-1111-111111111111","group":["ops"]}""");

        var token = UserToken.FromJsonElement(document.RootElement);

        token.HasGroup("ops").Should().BeTrue();
    }
}
