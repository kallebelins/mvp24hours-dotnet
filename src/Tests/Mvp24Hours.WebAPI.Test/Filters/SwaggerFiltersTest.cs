using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Filters.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mvp24Hours.WebAPI.Test.Filters;

[Trait("Category", "Unit")]
public class SwaggerFiltersTest
{
    [Fact]
    public void CustomSwaggerFilter_Should_KeepOnlyPublicRoutes()
    {
        var document = new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/api/public/orders"] = new OpenApiPathItem(),
                ["/api/private/orders"] = new OpenApiPathItem()
            }
        };

        var sut = new CustomSwaggerFilter();
        sut.Apply(document, null!);

        document.Paths.Should().ContainKey("/api/public/orders");
        document.Paths.Should().NotContainKey("/api/private/orders");
    }

    [Fact]
    public void VersionedSwaggerDocumentFilter_Should_NotThrow()
    {
        var document = new OpenApiDocument();
        var sut = new VersionedSwaggerDocumentFilter(new SwaggerOptions());

        Action act = () => sut.Apply(document, null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void VersionedSwaggerOperationFilter_Should_NotThrow()
    {
        var operation = new OpenApiOperation();
        var sut = new VersionedSwaggerOperationFilter();

        Action act = () => sut.Apply(operation, null!);

        act.Should().NotThrow();
    }

    [Fact]
    public void AuthResponsesOperationFilter_Should_ExposeConfiguredTypes()
    {
        var sut = new AuthResponsesOperationFilter([typeof(AuthorizeAttribute)]);

        sut.AuthTypes.Should().Contain(typeof(AuthorizeAttribute));
    }

    [Fact]
    public void DeprecationOperationFilter_Should_CreateInstance()
    {
        var sut = new DeprecationOperationFilter();

        sut.Should().NotBeNull();
    }

    [Fact]
    public void ExamplesOperationFilter_Should_CreateInstance()
    {
        var sut = new ExamplesOperationFilter();

        sut.Should().NotBeNull();
    }
}
