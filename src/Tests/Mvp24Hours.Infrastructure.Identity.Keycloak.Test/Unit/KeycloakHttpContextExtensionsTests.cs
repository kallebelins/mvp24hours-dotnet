using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.Contract.Infrastructure;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Core.ValueObjects.Authorization;
using Mvp24Hours.Infrastructure.Identity.Keycloak.Infrastructure.Extensions;

namespace Mvp24Hours.Infrastructure.Identity.Keycloak.Test.Unit;

[Trait("Category", "Unit")]
public sealed class KeycloakHttpContextExtensionsTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void GetAuthorization_ShouldReturnHeaderValue()
    {
        DefaultHttpContext context = new();
        context.Request.Headers.Authorization = "Bearer token-value";
        IHttpContextAccessor accessor = CreateAccessor(context);

        accessor.GetAuthorization().Should().Be("Bearer token-value");
    }

    [Fact]
    public void GetAuthorization_WithMissingContext_ShouldReturnNull()
    {
        IHttpContextAccessor accessor = CreateAccessor(null);

        accessor.GetAuthorization().Should().BeNull();
    }

    [Fact]
    public void GetUserToken_FromItems_ShouldReturnCachedUser()
    {
        UserToken user = new() { Id = UserId, PreferredUserName = "alice" };
        DefaultHttpContext context = new();
        context.Items[KeycloakHttpContextKeys.User] = user;
        IHttpContextAccessor accessor = CreateAccessor(context);

        accessor.GetUserToken().Should().BeSameAs(user);
    }

    [Fact]
    public void GetUserToken_ShouldParseBearerTokenWhenItemsAreEmpty()
    {
        UserToken parsed = new() { Id = UserId };
        Mock<IKeycloakJwtTokenParser> parser = new();
        parser.Setup(value => value.ParseUserToken("Bearer jwt"))
            .Returns(parsed);
        DefaultHttpContext context = new();
        context.Request.Headers.Authorization = "Bearer jwt";
        IHttpContextAccessor accessor = CreateAccessor(context);
        context.RequestServices = CreateServiceProvider(accessor, parser.Object);

        accessor.GetUserToken().Should().BeSameAs(parsed);
    }

    [Fact]
    public void GetUserToken_FromServiceProvider_ShouldResolveAccessor()
    {
        UserToken user = new() { Id = UserId };
        DefaultHttpContext context = new();
        context.Items[KeycloakHttpContextKeys.User] = user;
        ServiceCollection services = new();
        services.AddSingleton<IHttpContextAccessor>(CreateAccessor(context));
        ServiceProvider provider = services.BuildServiceProvider();

        provider.GetUserToken().Should().BeSameAs(user);
    }

    [Fact]
    public void GetUserId_ShouldReturnSubjectFromParsedToken()
    {
        UserToken user = new() { Id = UserId };
        DefaultHttpContext context = new();
        context.Items[KeycloakHttpContextKeys.User] = user;
        IHttpContextAccessor accessor = CreateAccessor(context);

        accessor.GetUserId().Should().Be(UserId);
        CreateServiceProvider(accessor).GetUserId().Should().Be(UserId);
    }

    private static IHttpContextAccessor CreateAccessor(HttpContext? context)
    {
        Mock<IHttpContextAccessor> accessor = new();
        accessor.Setup(value => value.HttpContext).Returns(context);
        return accessor.Object;
    }

    private static IServiceProvider CreateServiceProvider(params object[] services)
    {
        ServiceCollection collection = new();
        foreach (object service in services)
        {
            if (service is IHttpContextAccessor accessor)
            {
                collection.AddSingleton(accessor);
                continue;
            }

            collection.AddSingleton(service.GetType().GetInterfaces().FirstOrDefault() ?? service.GetType(), service);
        }

        return collection.BuildServiceProvider();
    }
}
