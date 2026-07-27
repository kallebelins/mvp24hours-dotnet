using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakOptionsTests
{
    [Fact]
    public void Validate_WithValidConfiguration_ShouldReturnNoErrors()
    {
        KeycloakOptions options = new()
        {
            Authority = "https://identity.example/realms/test",
            Realm = "test",
            ClientId = "api",
            Audience = "api"
        };

        options.Validate().Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithMissingAndInvalidValues_ShouldDescribeEveryProblem()
    {
        KeycloakOptions options = new()
        {
            Authority = "http://identity.example/realms/test",
            TokenClockSkew = TimeSpan.FromSeconds(-1),
            DiscoveryCacheTtl = TimeSpan.Zero,
            MetadataAddress = "relative"
        };

        IReadOnlyList<string> errors = options.Validate();

        errors.Should().Contain(error => error.Contains("HTTPS"));
        errors.Should().Contain(error => error.Contains("Realm"));
        errors.Should().Contain(error => error.Contains("ClientId"));
        errors.Should().Contain(error => error.Contains("Audience"));
        errors.Should().Contain(error => error.Contains("TokenClockSkew"));
        errors.Should().Contain(error => error.Contains("DiscoveryCacheTtl"));
        errors.Should().Contain(error => error.Contains("MetadataAddress"));
    }

    [Fact]
    public void AuthorizationOptions_ShouldRejectSimultaneousDecisionAndRpt()
    {
        KeycloakAuthorizationOptions options = new()
        {
            UseDecisionEndpoint = true,
            UseRptEndpoint = true,
            ResourceServerClientId = "api"
        };

        options.Validate().Should().ContainSingle(error =>
            error.Contains("cannot both be true"));
    }

    [Fact]
    public void AdminOptions_ShouldRequireSecretForServiceAccount()
    {
        KeycloakAdminOptions options = new()
        {
            AdminBaseUrl = "https://identity.example/admin/realms/test",
            Realm = "test",
            ClientId = "admin-client"
        };

        options.Validate().Should().ContainSingle(error =>
            error.Contains("ClientSecret"));
    }
}
