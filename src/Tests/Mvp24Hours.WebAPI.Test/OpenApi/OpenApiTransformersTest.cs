using Microsoft.OpenApi;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.OpenApi;

namespace Mvp24Hours.WebAPI.Test.OpenApi;

[Trait("Category", "Unit")]
public class OpenApiTransformersTest
{
    [Fact]
    public async Task SecuritySchemeTransformer_Should_AddBearerScheme()
    {
        var options = new NativeOpenApiOptions
        {
            AuthenticationScheme = OpenApiAuthenticationScheme.Bearer
        };
        var document = new OpenApiDocument();
        var sut = new SecuritySchemeTransformer(options);

        await sut.TransformAsync(document, null!, CancellationToken.None);

        document.Components.Should().NotBeNull();
        document.Components!.SecuritySchemes.Should().ContainKey("Bearer");
    }

    [Fact]
    public async Task CustomHeadersTransformer_Should_AddHeadersToOperations()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        var sut = new CustomHeadersTransformer(("X-Correlation-ID", "Correlation", false));

        await sut.TransformAsync(document, null!, CancellationToken.None);

        OpenApiOperation operation = GetOrdersOperation(document);
        operation.Parameters.Should().Contain(x => x.Name == "X-Correlation-ID");
    }

    [Fact]
    public async Task CommonResponsesTransformer_Should_AddConfiguredResponses()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        var sut = new CommonResponsesTransformer(add401: true, add403: true, add500: true);

        await sut.TransformAsync(document, null!, CancellationToken.None);

        OpenApiResponses? responses = GetOrdersOperation(document).Responses;
        responses.Should().ContainKey("401");
        responses.Should().ContainKey("403");
        responses.Should().ContainKey("500");
    }

    [Fact]
    public async Task ProblemDetailsTransformer_Should_AddSchemasAndResponseContent()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        GetOrdersOperation(document).Responses!["400"] = new OpenApiResponse { Description = "Bad Request" };
        var sut = new ProblemDetailsTransformer();

        await sut.TransformAsync(document, null!, CancellationToken.None);

        document.Components!.Schemas.Should().ContainKey("ProblemDetails");
        document.Components!.Schemas.Should().ContainKey("ValidationProblemDetails");
        GetOrdersOperation(document).Responses!["400"]!.Content.Should().ContainKey("application/problem+json");
    }

    [Fact]
    public async Task RateLimitHeadersTransformer_Should_AddRateLimitHeadersAnd429()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        var sut = new RateLimitHeadersTransformer();

        await sut.TransformAsync(document, null!, CancellationToken.None);

        GetOrdersOperation(document).Responses.Should().ContainKey("429");
    }

    [Fact]
    public async Task TagFilterTransformer_Should_RemoveOperationsOutsideIncludedTags()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        OpenApiOperation operation = GetOrdersOperation(document);
        operation.Tags = new HashSet<OpenApiTagReference>
        {
            new("Orders", document)
        };
        var sut = new TagFilterTransformer(includeTags: ["Billing"]);

        await sut.TransformAsync(document, null!, CancellationToken.None);

        document.Paths.Should().BeEmpty();
    }

    [Fact]
    public async Task DeprecationTransformer_Should_AddWarningToDeprecatedOperations()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        OpenApiOperation operation = GetOrdersOperation(document);
        operation.Deprecated = true;
        operation.Description = "Original description";
        var sut = new DeprecationTransformer(
            "This endpoint will be removed soon.",
            new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        await sut.TransformAsync(document, null!, CancellationToken.None);

        operation.Description.Should().Contain("DEPRECATED");
        operation.Description.Should().Contain("This endpoint will be removed soon.");
        operation.Description.Should().Contain("2026-12-31");
        operation.Description.Should().Contain("Original description");
    }

    [Fact]
    public async Task DeprecationTransformer_Should_LeaveNonDeprecatedOperationsUnchanged()
    {
        OpenApiDocument document = CreateDocumentWithSingleOperation();
        OpenApiOperation operation = GetOrdersOperation(document);
        operation.Description = "Active endpoint";
        var sut = new DeprecationTransformer();

        await sut.TransformAsync(document, null!, CancellationToken.None);

        operation.Description.Should().Be("Active endpoint");
    }

    [Fact]
    public async Task DeprecationTransformer_Should_HandleNullPaths()
    {
        var document = new OpenApiDocument { Paths = null };
        var sut = new DeprecationTransformer();

        Func<Task> act = () => sut.TransformAsync(document, null!, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static OpenApiOperation GetOrdersOperation(OpenApiDocument document)
    {
        return document.Paths!["/api/orders"]!.Operations!.Values.Single();
    }

    private static OpenApiDocument CreateDocumentWithSingleOperation()
    {
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "ok" } }
        };

        return new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/api/orders"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = operation
                    }
                }
            }
        };
    }
}
