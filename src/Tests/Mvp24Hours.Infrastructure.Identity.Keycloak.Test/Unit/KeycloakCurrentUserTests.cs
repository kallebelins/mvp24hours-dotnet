using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Application.Logic;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakCurrentUserTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void User_ShouldResolveFromHttpContextItems()
    {
        UserToken user = new() { Id = UserId, PreferredUserName = "carol" };
        DefaultHttpContext context = new();
        context.Items[KeycloakHttpContextKeys.User] = user;
        KeycloakCurrentUser currentUser = new(CreateAccessor(context));

        currentUser.User.Should().BeSameAs(user);
        currentUser.UserId.Should().Be(UserId);
    }

    [Fact]
    public void IsAuthenticated_ShouldReflectIdentityState()
    {
        DefaultHttpContext authenticated = new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "carol")], "Bearer"))
        };
        DefaultHttpContext anonymous = new();
        KeycloakCurrentUser authenticatedUser = new(CreateAccessor(authenticated));
        KeycloakCurrentUser anonymousUser = new(CreateAccessor(anonymous));

        authenticatedUser.IsAuthenticated.Should().BeTrue();
        anonymousUser.IsAuthenticated.Should().BeFalse();
        anonymousUser.User.Should().BeNull();
        anonymousUser.UserId.Should().BeNull();
    }

    private static IHttpContextAccessor CreateAccessor(HttpContext? context)
    {
        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(value => value.HttpContext).Returns(context);
        return accessor.Object;
    }
}
