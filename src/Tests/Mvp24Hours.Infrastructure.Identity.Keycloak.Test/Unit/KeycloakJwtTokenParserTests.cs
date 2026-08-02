using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Options;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakJwtTokenParserTests
{
    private static readonly Guid UserId = Guid.Parse(
        "11111111-1111-1111-1111-111111111111");

    [Fact]
    public void ParseUserToken_ShouldReadBearerJwtClaims()
    {
        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddMinutes(10);
        string jwt = CreateJwt(
            expiration,
            new Claim("sub", UserId.ToString()),
            new Claim("preferred_username", "alice"),
            new Claim("email", "alice@example.test"),
            new Claim("email_verified", "true"),
            new Claim("groups", "[\"engineering\"]", JsonClaimValueTypes.JsonArray),
            new Claim(
                "realm_access",
                "{\"roles\":[\"admin\"]}",
                JsonClaimValueTypes.Json));
        KeycloakJwtTokenParser parser = CreateParser();

        UserToken? token = parser.ParseUserToken($"Bearer {jwt}");

        token.Should().NotBeNull();
        token!.Id.Should().Be(UserId);
        token.PreferredUserName.Should().Be("alice");
        token.EmailVerified.Should().BeTrue();
        token.HasRealmRole("ADMIN").Should().BeTrue();
        token.HasGroup("engineering").Should().BeTrue();
        parser.ParseClaims(jwt).Should().ContainKey("sub");
        parser.IsExpired(jwt).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public void ParseUserToken_WithInvalidInput_ShouldReturnNull(string? value)
    {
        KeycloakJwtTokenParser parser = CreateParser();

        parser.ParseUserToken(value).Should().BeNull();
        parser.ParseUserId(value).Should().BeNull();
        parser.GetExpiration(value).Should().BeNull();
        parser.IsExpired(value).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_ShouldHonorConfiguredClockSkew()
    {
        string jwt = CreateJwt(DateTimeOffset.UtcNow.AddSeconds(-2));
        KeycloakJwtTokenParser parser = CreateParser(TimeSpan.FromSeconds(10));

        parser.IsExpired(jwt).Should().BeFalse();
    }

    private static KeycloakJwtTokenParser CreateParser(TimeSpan? clockSkew = null)
    {
        return new KeycloakJwtTokenParser(
            Options.Create(new KeycloakOptions
            {
                TokenClockSkew = clockSkew ?? TimeSpan.Zero
            }),
            NullLogger<KeycloakJwtTokenParser>.Instance);
    }

    private static string CreateJwt(
        DateTimeOffset expiration,
        params Claim[] claims)
    {
        JwtSecurityToken token = new(
            claims: claims,
            expires: expiration.UtcDateTime);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
