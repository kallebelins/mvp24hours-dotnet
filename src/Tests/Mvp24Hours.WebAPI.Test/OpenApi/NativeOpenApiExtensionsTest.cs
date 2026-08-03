using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Extensions;
using Mvp24Hours.WebAPI.OpenApi;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.OpenApi;

[Trait("Category", "Unit")]
public class NativeOpenApiExtensionsTest
{
    #region Service registration

    [Fact]
    public void AddMvp24HoursNativeOpenApi_Should_RegisterOptionsAndOpenApiServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursNativeOpenApi(options =>
        {
            options.Title = "Coverage API";
            options.Version = "2.0.0";
            options.Description = "Block D coverage";
            options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
            options.Contact = new OpenApiContactInfo { Name = "Support", Email = "support@test.com", Url = "https://test.com" };
            options.License = new OpenApiLicenseInfo { Name = "MIT", Url = "https://opensource.org/licenses/MIT" };
            options.TermsOfServiceUrl = "https://test.com/tos";
            options.IncludeServerInfo = true;
            options.Servers =
            [
                new OpenApiServerInfo
                {
                    Url = "https://api.test.com/{version}",
                    Description = "Primary",
                    Variables = new Dictionary<string, Mvp24Hours.WebAPI.Configuration.OpenApiServerVariable>
                    {
                        ["version"] = new Mvp24Hours.WebAPI.Configuration.OpenApiServerVariable { Default = "v1", Description = "API version" }
                    }
                }
            ];
            options.Tags = [new OpenApiTagInfo { Name = "Orders", Description = "Order endpoints" }];
            options.ExternalDocsUrl = "https://docs.test.com";
            options.ExternalDocsDescription = "External docs";
        });

        ServiceProvider provider = services.BuildServiceProvider();
        NativeOpenApiOptions options = provider.GetRequiredService<NativeOpenApiOptions>();

        options.Title.Should().Be("Coverage API");
        options.Version.Should().Be("2.0.0");
        services.Should().Contain(x => x.ServiceType == typeof(NativeOpenApiOptions));
    }

    [Fact]
    public void AddMvp24HoursNativeOpenApiWithVersions_Should_RegisterAdditionalDocuments()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursNativeOpenApiWithVersions(options =>
        {
            options.Title = "Versioned API";
            options.AdditionalVersions.Add(new OpenApiVersionConfig
            {
                DocumentName = "v2",
                Version = "2.0.0",
                Title = "Versioned API v2",
                IsDeprecated = true,
                DeprecationMessage = "Use v3 instead"
            });
        });

        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<NativeOpenApiOptions>().AdditionalVersions.Should().ContainSingle(v => v.DocumentName == "v2");
    }

    [Fact]
    public void AddMvp24HoursNativeOpenApiMinimal_Should_EnableSwaggerUiByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursNativeOpenApiMinimal("Minimal API", "1.2.3");

        NativeOpenApiOptions options = services.BuildServiceProvider().GetRequiredService<NativeOpenApiOptions>();
        options.Title.Should().Be("Minimal API");
        options.Version.Should().Be("1.2.3");
        options.EnableSwaggerUI.Should().BeTrue();
    }

    #endregion

    #region Application builder pipeline

    [Fact]
    public void UseMvp24HoursNativeOpenApi_Should_UseRegisteredOptions()
    {
        using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigureServices(services => services.AddMvp24HoursNativeOpenApi(options =>
                {
                    options.EnableSwaggerUI = true;
                    options.SwaggerUIRoutePrefix = "docs";
                }))
            .ConfigurePipeline(app => app.UseMvp24HoursNativeOpenApi());

        factory.CreateClient().Should().NotBeNull();
    }

    [Fact]
    public void UseMvp24HoursNativeOpenApi_WithCustomOptions_ShouldConfigureSwaggerUi()
    {
        var options = new NativeOpenApiOptions
        {
            EnableSwaggerUI = true,
            SwaggerUIRoutePrefix = "swagger-ui",
            Title = "Custom",
            Version = "1.0.0",
            AdditionalVersions =
            [
                new OpenApiVersionConfig
                {
                    DocumentName = "legacy",
                    Version = "0.9.0",
                    Title = "Legacy",
                    IsDeprecated = true
                }
            ]
        };

        using WebApiTestApplicationFactory factory = new WebApiTestApplicationFactory()
            .ConfigurePipeline(app => app.UseMvp24HoursNativeOpenApi(options));

        factory.CreateClient().Should().NotBeNull();
    }

    #endregion

    #region WebApplication endpoint mapping

    [Fact]
    public async Task MapMvp24HoursNativeOpenApi_Should_ExposeIndexAndDocumentationEndpoints()
    {
        await using WebApplication app = await CreateMappedApplicationAsync(options =>
        {
            options.EnableSwaggerUI = true;
            options.EnableReDoc = true;
            options.AdditionalVersions.Add(new OpenApiVersionConfig
            {
                DocumentName = "v2",
                Version = "2.0.0",
                Title = "API v2",
                IsDeprecated = true
            });
        });

        HttpClient client = app.GetTestClient();

        HttpResponseMessage indexResponse = await client.GetAsync("/openapi");
        indexResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string indexJson = await indexResponse.Content.ReadAsStringAsync();
        indexJson.Should().Contain("documents");

        HttpResponseMessage openApiResponse = await client.GetAsync("/openapi/v1.json");
        openApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage v2Response = await client.GetAsync("/openapi/v2.json");
        v2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage swaggerStandalone = await client.GetAsync("/swagger/standalone");
        swaggerStandalone.StatusCode.Should().Be(HttpStatusCode.OK);
        (await swaggerStandalone.Content.ReadAsStringAsync()).Should().Contain("SwaggerUIBundle");

        HttpResponseMessage redocRedirect = await client.GetAsync("/redoc");
        redocRedirect.StatusCode.Should().Be(HttpStatusCode.Redirect);

        HttpResponseMessage redocHtml = await client.GetAsync("/redoc/index.html");
        redocHtml.StatusCode.Should().Be(HttpStatusCode.OK);
        (await redocHtml.Content.ReadAsStringAsync()).Should().Contain("<redoc");
    }

    [Fact]
    public async Task MapMvp24HoursNativeOpenApi_WithDefaultOptions_Should_ReturnDocumentList()
    {
        await using WebApplication app = await CreateMappedApplicationAsync();

        HttpClient client = app.GetTestClient();
        HttpResponseMessage response = await client.GetAsync("/openapi");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("documents").GetArrayLength().Should().BeGreaterThan(0);
    }

    #endregion

    #region SecuritySchemeTransformer

    [Theory]
    [InlineData(OpenApiAuthenticationScheme.Bearer, "Bearer")]
    [InlineData(OpenApiAuthenticationScheme.Basic, "Basic")]
    [InlineData(OpenApiAuthenticationScheme.ApiKey, "ApiKey")]
    [InlineData(OpenApiAuthenticationScheme.OAuth2, "OAuth2")]
    public async Task SecuritySchemeTransformer_Should_AddConfiguredScheme(
        OpenApiAuthenticationScheme scheme,
        string expectedSchemeName)
    {
        var options = new NativeOpenApiOptions
        {
            AuthenticationScheme = scheme,
            BearerSecurityScheme = new OpenApiBearerSecurityScheme { Scheme = "Bearer", BearerFormat = "JWT" },
            ApiKeySecurityScheme = new OpenApiApiKeySecurityScheme
            {
                Name = "X-Api-Key",
                Location = ApiKeyLocation.Query,
                Description = "Query API key"
            }
        };
        var document = new OpenApiDocument();
        var transformer = new SecuritySchemeTransformer(options);

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.Should().NotBeNull();
        document.Components!.SecuritySchemes.Should().ContainKey(expectedSchemeName);
        document.Security.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SecuritySchemeTransformer_None_Should_NotAddSecuritySchemes()
    {
        var options = new NativeOpenApiOptions { AuthenticationScheme = OpenApiAuthenticationScheme.None };
        var document = new OpenApiDocument();
        var transformer = new SecuritySchemeTransformer(options);

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components?.SecuritySchemes.Should().BeNullOrEmpty();
    }

    #endregion

    private static async Task<WebApplication> CreateMappedApplicationAsync(Action<NativeOpenApiOptions>? configure = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddLogging();
        builder.Services.AddMvp24HoursNativeOpenApi(options =>
        {
            options.Title = "Test API";
            options.Version = "1.0.0";
            options.EnableSwaggerUI = true;
            options.EnableReDoc = true;
            configure?.Invoke(options);
        });

        WebApplication app = builder.Build();
        app.MapGet("/api/ping", () => Results.Ok("pong"));
        app.MapMvp24HoursNativeOpenApi();
        await app.StartAsync();
        return app;
    }
}
