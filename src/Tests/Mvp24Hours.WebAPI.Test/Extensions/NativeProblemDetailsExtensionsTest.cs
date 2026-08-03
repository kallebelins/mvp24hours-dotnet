using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Extensions;

[Trait("Category", "Unit")]
public class NativeProblemDetailsExtensionsTest
{
    [Fact]
    public void AddNativeProblemDetails_Should_RegisterMappersAndProblemDetailsService()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNativeProblemDetails(options => options.ProblemTypeBaseUri = "https://errors.test");

        services.Should().Contain(x => x.ServiceType == typeof(DefaultExceptionToProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(ValidationProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType == typeof(IExceptionToProblemDetailsMapper));
        services.Should().Contain(x => x.ServiceType.Name.Contains("IProblemDetailsService"));
    }

    [Fact]
    public void AddNativeProblemDetailsAll_Should_SetDevelopmentOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Development };

        services.AddNativeProblemDetailsAll(environment, options => options.FallbackDetail = "fallback");

        MvpProblemDetailsOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvpProblemDetailsOptions>>().Value;

        options.IncludeExceptionDetails.Should().BeTrue();
        options.IncludeStackTrace.Should().BeTrue();
        options.FallbackDetail.Should().Be("fallback");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return404ProblemDetails_ForNotFoundException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new NotFoundException("missing", "Order", 1)));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("missing");
        body.Should().Contain("traceId");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_IncludeCorrelationId_FromHeader()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new Core.Exceptions.ValidationException("invalid")));

        HttpClient client = host.GetTestClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", "corr-99");
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("corr-99");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_WriteStatusCodePage_ForNonException404()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/missing", async context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await Task.CompletedTask;
            }));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/missing");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Not Found");
    }

    [Fact]
    public void UseNativeProblemDetailsHandling_Should_ReturnWebApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddNativeProblemDetails();
        WebApplication app = builder.Build();

        WebApplication result = app.UseNativeProblemDetailsHandling();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void AddNativeProblemDetailsAll_ProductionEnvironment_ShouldNotIncludeStackTrace()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var environment = new TestHostEnvironment { EnvironmentName = Environments.Production };

        services.AddNativeProblemDetailsAll(environment);

        MvpProblemDetailsOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvpProblemDetailsOptions>>().Value;

        options.IncludeExceptionDetails.Should().BeFalse();
        options.IncludeStackTrace.Should().BeFalse();
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return409ProblemDetails_ForConflictException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new ConflictException("duplicate", "Order", "Name")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("duplicate");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return422ProblemDetails_ForDomainException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new DomainException("rule violated", "Order")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadAsStringAsync()).Should().Contain("rule violated");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_UseCorrelationId_FromHttpContextItems()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext context) =>
            {
                context.Items["CorrelationId"] = "item-corr-1";
                throw new Core.Exceptions.ValidationException("invalid");
            }));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("item-corr-1");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return401ProblemDetails_ForUnauthorizedException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new UnauthorizedException("login required", "Bearer")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().Contain("authenticationScheme");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return403ProblemDetails_ForForbiddenException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new ForbiddenException("denied", "Order", "delete", "orders:delete")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await response.Content.ReadAsStringAsync()).Should().Contain("requiredPermission");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return500ProblemDetails_ForGenericException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options =>
                {
                    options.IncludeExceptionDetails = true;
                    options.LogExceptions = true;
                }),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new Exception("unexpected")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).Should().Contain("traceId");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return408ProblemDetails_ForTimeoutException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new TimeoutException("timed out")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return499ProblemDetails_ForOperationCanceledException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new OperationCanceledException("cancelled")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        ((int)response.StatusCode).Should().Be(499);
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_ReturnStatusCodePage_For405()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/only-get", () => Results.Ok()));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.PostAsync("/only-get", null);

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Method Not Allowed");
    }

    [Fact]
    public void AddNativeProblemDetails_WithoutConfigureAction_ShouldRegisterDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNativeProblemDetails();

        MvpProblemDetailsOptions options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<MvpProblemDetailsOptions>>().Value;

        options.Should().NotBeNull();
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return422ProblemDetails_ForBusinessException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new BusinessException("business rule", "BR001")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be((HttpStatusCode)422);
        (await response.Content.ReadAsStringAsync()).Should().Contain("errorCode");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return400ProblemDetails_ForArgumentException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options =>
            {
                options.IncludeExceptionDetails = true;
                options.ProblemTypeBaseUri = "https://errors.test";
            }),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new ArgumentException("bad arg")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("bad arg");
        body.Should().Contain("https://errors.test/invalid-argument");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_Return501ProblemDetails_ForNotImplementedException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new NotImplementedException("todo")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
        (await response.Content.ReadAsStringAsync()).Should().Contain("todo");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_UseFallbackDetail_ForGenericExceptionInProduction()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options =>
            {
                options.IncludeExceptionDetails = false;
                options.FallbackDetail = "Something went wrong";
            }),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new Exception("secret details")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Something went wrong");
        body.Should().NotContain("secret details");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_IncludeValidationErrors_ForValidationException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new Core.Exceptions.ValidationException(
                    "invalid",
                    [new MessageResult("Name", "Required", MessageType.Error)])));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("validationErrors");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_IncludeEntityExtensions_ForNotFoundException()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new NotFoundException("missing", "Order", 42)));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("entityName");
        body.Should().Contain("entityId");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_ReturnStatusCodePage_For429()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/limited", async context =>
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await Task.CompletedTask;
            }));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/limited");

        response.StatusCode.Should().Be((HttpStatusCode)429);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Too Many Requests");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_ReturnStatusCodePage_For503()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(),
            endpoints => endpoints.MapGet("/down", async context =>
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await Task.CompletedTask;
            }));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/down");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Service Unavailable");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_IncludeStackTrace_WhenConfigured()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options =>
            {
                options.IncludeExceptionDetails = true;
                options.IncludeStackTrace = true;
            }),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) => throw new InvalidOperationException("invalid op")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("stackTrace");
    }

    [Fact]
    public async Task UseNativeProblemDetailsHandling_Should_IncludeDomainRuleName()
    {
        using IHost host = await CreateHost(
            app => app.UseNativeProblemDetailsHandling(),
            services => services.AddNativeProblemDetails(options => options.IncludeExceptionDetails = true),
            endpoints => endpoints.MapGet("/fail", (HttpContext _) =>
                throw new DomainException("rule violated", "Order", "MaxItemsRule")));

        HttpClient client = host.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/fail");

        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("ruleName");
        body.Should().Contain("MaxItemsRule");
    }

    private static async Task<IHost> CreateHost(
        Action<IApplicationBuilder> configureApp,
        Action<IServiceCollection> configureServices,
        Action<IEndpointRouteBuilder> configureEndpoints)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging();
                    configureServices(services);
                })
                .Configure(app =>
                {
                    configureApp(app);
                    app.UseRouting();
                    app.UseEndpoints(configureEndpoints);
                }))
            .StartAsync();
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = "Mvp24Hours.WebAPI.Test";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
