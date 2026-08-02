using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Authentication;

[Trait("Category", "Unit")]
public class AuthenticationTest
{
    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationOptions
    // -----------------------------------------------------------------------

    [Fact]
    public void ApiKeyAuthenticationOptions_Should_HaveExpectedDefaults()
    {
        var sut = new ApiKeyAuthenticationOptions();

        sut.HeaderName.Should().Be("X-Api-Key");
        sut.QueryParameterName.Should().Be("api_key");
        sut.EnableHeaderKey.Should().BeTrue();
        sut.EnableQueryStringKey.Should().BeFalse();
        sut.RequireAuthenticationByDefault.Should().BeTrue();
        sut.ChallengeScheme.Should().Be("ApiKey");
        sut.Realm.Should().Be("API");
    }

    [Fact]
    public void ApiKeyAuthenticationOptions_Should_ExcludeSwaggerAndHealthByDefault()
    {
        var sut = new ApiKeyAuthenticationOptions();

        sut.ExcludedPaths.Should().Contain("/health");
        sut.ExcludedPaths.Should().Contain("/swagger");
    }

    [Fact]
    public void ApiKeyValidationResult_Success_Should_SetIsValid()
    {
        var result = ApiKeyValidationResult.Success("key-id", "client-1");

        result.IsValid.Should().BeTrue();
        result.KeyIdentifier.Should().Be("key-id");
        result.ClientId.Should().Be("client-1");
    }

    [Fact]
    public void ApiKeyValidationResult_Failure_Should_SetFailureReason()
    {
        var result = ApiKeyValidationResult.Failure("bad key");

        result.IsValid.Should().BeFalse();
        result.FailureReason.Should().Be("bad key");
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — valid key
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_CallNext_WhenValidKeyProvided()
    {
        bool called = false;
        var options = new ApiKeyAuthenticationOptions();
        options.ApiKeys.Add("secret-key");

        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Request.Headers["X-Api-Key"] = "secret-key";

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_SetPrincipal_WhenValidKeyProvided()
    {
        var options = new ApiKeyAuthenticationOptions();
        options.ApiKeys.Add("secret-key");
        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ => Task.CompletedTask);

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Request.Headers["X-Api-Key"] = "secret-key";

        await sut.InvokeAsync(context);

        context.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — missing key
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_Return401_WhenKeyMissing()
    {
        var options = new ApiKeyAuthenticationOptions();
        options.ApiKeys.Add("secret-key");
        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ => Task.CompletedTask);

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — invalid key
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_Return401_WhenKeyInvalid()
    {
        var options = new ApiKeyAuthenticationOptions();
        options.ApiKeys.Add("valid-key");
        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ => Task.CompletedTask);

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Request.Headers["X-Api-Key"] = "wrong-key";

        await sut.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(401);
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — excluded paths
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_Bypass_ForExcludedPath()
    {
        bool called = false;
        var options = new ApiKeyAuthenticationOptions();
        options.ApiKeys.Add("secret-key");

        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/health");

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(401);
    }

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_Bypass_WhenRequireAuthFalseAndNotProtected()
    {
        bool called = false;
        var options = new ApiKeyAuthenticationOptions
        {
            RequireAuthenticationByDefault = false
        };

        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/public");

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — query string
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_AcceptKeyFromQueryString()
    {
        bool called = false;
        var options = new ApiKeyAuthenticationOptions
        {
            EnableQueryStringKey = true,
            EnableHeaderKey = false
        };
        options.ApiKeys.Add("qs-key");

        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Request.QueryString = new Microsoft.AspNetCore.Http.QueryString("?api_key=qs-key");

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — custom validator
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_UseCustomValidator()
    {
        bool called = false;
        var options = new ApiKeyAuthenticationOptions
        {
            CustomValidator = key => Task.FromResult(
                key == "custom-valid" ? ApiKeyValidationResult.Success("id") : ApiKeyValidationResult.Failure("nope"))
        };

        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Request.Headers["X-Api-Key"] = "custom-valid";

        await sut.InvokeAsync(context);

        called.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — scopes
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_AddScopeClaims()
    {
        var options = new ApiKeyAuthenticationOptions();
        options.ApiKeys.Add("scoped-key");
        options.ApiKeyScopes["scoped-key"] = ["read", "write"];

        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ => Task.CompletedTask);

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");
        context.Request.Headers["X-Api-Key"] = "scoped-key";

        await sut.InvokeAsync(context);

        context.User.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "read");
        context.User.Claims.Should().Contain(c => c.Type == "scope" && c.Value == "write");
    }

    // -----------------------------------------------------------------------
    // ApiKeyAuthenticationMiddleware — WWW-Authenticate header
    // -----------------------------------------------------------------------

    [Fact]
    public async Task ApiKeyAuthenticationMiddleware_Should_SetWwwAuthenticateHeader_OnUnauthorized()
    {
        var options = new ApiKeyAuthenticationOptions();
        ApiKeyAuthenticationMiddleware sut = CreateMiddleware(options, _ => Task.CompletedTask);

        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext(path: "/api/data");

        await sut.InvokeAsync(context);

        context.Response.Headers.Should().ContainKey("WWW-Authenticate");
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    private static ApiKeyAuthenticationMiddleware CreateMiddleware(
        ApiKeyAuthenticationOptions options,
        RequestDelegate next)
    {
        return new ApiKeyAuthenticationMiddleware(
            next,
            Options.Create(options),
            NullLogger<ApiKeyAuthenticationMiddleware>.Instance);
    }
}
