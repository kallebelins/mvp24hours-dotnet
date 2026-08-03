using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public void DeprecationOperationFilter_Should_MarkObsoleteActionAsDeprecated()
    {
        var operation = new OpenApiOperation();
        MethodInfo method = typeof(DeprecationTestController).GetMethod(nameof(DeprecationTestController.ObsoleteAction))!;
        var context = new OperationFilterContext(null!, null!, null!, null!, method);
        var sut = new DeprecationOperationFilter();

        sut.Apply(operation, context);

        operation.Deprecated.Should().BeTrue();
        operation.Description.Should().Contain("DEPRECATED");
        operation.Description.Should().Contain("Use v2 instead");
        operation.Extensions.Should().ContainKey("x-deprecation-warning");
    }

    [Fact]
    public void DeprecationOperationFilter_Should_MarkDeprecatedApiVersionOnController()
    {
        var operation = new OpenApiOperation();
        MethodInfo method = typeof(DeprecatedVersionController).GetMethod(nameof(DeprecatedVersionController.Get))!;
        var context = new OperationFilterContext(null!, null!, null!, null!, method);
        var sut = new DeprecationOperationFilter();

        sut.Apply(operation, context);

        operation.Deprecated.Should().BeTrue();
        operation.Description.Should().Contain("deprecated");
    }

    [Fact]
    public void ExamplesOperationFilter_Should_GenerateExamplesFromSchemas()
    {
        var operation = new OpenApiOperation
        {
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["application/json"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "email" }
                    }
                }
            },
            Parameters =
            [
                new OpenApiParameter
                {
                    Schema = new OpenApiSchema { Type = JsonSchemaType.Integer }
                }
            ],
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = JsonSchemaType.Array,
                                Items = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri" }
                            }
                        }
                    }
                }
            }
        };
        var sut = new ExamplesOperationFilter();

        sut.Apply(operation, null!);

        operation.RequestBody!.Content!["application/json"].Example!.ToJsonString().Should().Contain("user@example.com");
        operation.Parameters!.Single().Example!.ToJsonString().Should().Be("0");
        operation.Responses["200"].Content!["application/json"].Example.Should().NotBeNull();
    }

    [Fact]
    public void ExamplesOperationFilter_Should_PreserveExistingExamples()
    {
        var operation = new OpenApiOperation
        {
            Parameters =
            [
                new OpenApiParameter
                {
                    Example = "existing",
                    Schema = new OpenApiSchema { Type = JsonSchemaType.String }
                }
            ]
        };
        var sut = new ExamplesOperationFilter();

        sut.Apply(operation, null!);

        operation.Parameters!.Single().Example!.ToJsonString().Should().Be("\"existing\"");
    }
}

[ApiVersion("1.0", Deprecated = true)]
internal sealed class DeprecatedVersionController
{
    public void Get()
    {
    }
}

internal sealed class DeprecationTestController
{
    [Obsolete("Use v2 instead")]
    public void ObsoleteAction()
    {
    }
}
