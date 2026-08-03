using Microsoft.AspNetCore.Http;
using Moq;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using Mvp24Hours.Infrastructure.Identity.Keycloak.WebAPI.Middlewares;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakCurrentUserMiddlewareTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task InvokeAsync_ShouldStoreParsedUserInItems()
    {
        UserToken user = new() { Id = UserId, PreferredUserName = "bob" };
        Mock<IKeycloakJwtTokenParser> parser = new();
        parser.Setup(value => value.ParseUserToken("Bearer jwt"))
            .Returns(user);
        DefaultHttpContext context = new();
        context.Request.Headers.Authorization = "Bearer jwt";
        bool nextCalled = false;
        KeycloakCurrentUserMiddleware middleware = new(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, parser.Object);

        nextCalled.Should().BeTrue();
        context.Items[KeycloakHttpContextKeys.User].Should().BeSameAs(user);
    }

    [Fact]
    public async Task InvokeAsync_WithUnparseableToken_ShouldNotStoreUser()
    {
        Mock<IKeycloakJwtTokenParser> parser = new();
        parser.Setup(value => value.ParseUserToken(It.IsAny<string?>()))
            .Returns((UserToken?)null);
        DefaultHttpContext context = new();
        context.Request.Headers.Authorization = "Bearer invalid";
        KeycloakCurrentUserMiddleware middleware = new(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, parser.Object);

        context.Items.ContainsKey(KeycloakHttpContextKeys.User).Should().BeFalse();
    }
}
