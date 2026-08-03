using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.WebAPI.Endpoints;

namespace Mvp24Hours.WebAPI.Test.Endpoints;

[Trait("Category", "Unit")]
public class TypedResultsExtensionsExtendedTest
{
    [Fact]
    public void ToTypedResult_Should_ReturnNotFound_WhenDataIsNull()
    {
        IBusinessResult<string> result = new BusinessResult<string>(null);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<NotFound<ProblemDetails>>();
    }

    [Fact]
    public void ToTypedResultAllowNull_Should_ReturnOk_WhenDataIsNull()
    {
        IBusinessResult<string> result = new BusinessResult<string>(null);

        IResult httpResult = result.ToTypedResultAllowNull();

        httpResult.Should().BeOfType<Ok>();
    }

    [Fact]
    public void ToTypedResult_Should_MapNotFoundStructuredError()
    {
        var messages = new List<IMessageResult>
        {
            StructuredMessageResult.NotFound("Order", 42)
        };
        IBusinessResult<object> result = new BusinessResult<object>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<NotFound<ProblemDetails>>();
    }

    [Fact]
    public void ToTypedResult_Should_MapConflictStructuredError()
    {
        var messages = new List<IMessageResult>
        {
            StructuredMessageResult.Conflict("Order", "Duplicate order number")
        };
        IBusinessResult<object> result = new BusinessResult<object>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<Conflict<ProblemDetails>>();
    }

    [Fact]
    public void ToTypedResult_Should_MapUnauthorizedStructuredError()
    {
        var messages = new List<IMessageResult> { StructuredMessageResult.Unauthorized() };
        IBusinessResult<object> result = new BusinessResult<object>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<UnauthorizedHttpResult>();
    }

    [Fact]
    public void ToTypedResult_Should_MapForbiddenStructuredError()
    {
        var messages = new List<IMessageResult> { StructuredMessageResult.Forbidden("Orders", "delete") };
        IBusinessResult<object> result = new BusinessResult<object>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public void ToTypedResult_Should_MapValidationStructuredError()
    {
        var messages = new List<IMessageResult>
        {
            StructuredMessageResult.Validation("Email", "Invalid format", "VALIDATION")
        };
        IBusinessResult<object> result = new BusinessResult<object>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<ProblemHttpResult>();
    }

    [Fact]
    public void ToProblem_Should_MapKnownExceptions()
    {
        ProblemHttpResult notFound = new NotFoundException("missing").ToProblem(includeDetails: true);
        ProblemHttpResult validation = new ValidationException("bad").ToProblem(includeDetails: true);
        ProblemHttpResult unauthorized = new UnauthorizedException("auth").ToProblem(includeDetails: true);
        ProblemHttpResult forbidden = new ForbiddenException("deny").ToProblem(includeDetails: true);
        ProblemHttpResult conflict = new ConflictException("dup").ToProblem(includeDetails: true);

        notFound.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        validation.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        unauthorized.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToProblemWithStackTrace_Should_IncludeStackTrace()
    {
        var exception = new InvalidOperationException("boom");

        ProblemHttpResult result = exception.ToProblemWithStackTrace();

        result.ProblemDetails.Extensions.Should().ContainKey("stackTrace");
    }

    [Fact]
    public void NotFoundProblem_Should_Create404Problem()
    {
        ProblemHttpResult result = TypedResultsExtensions.NotFoundProblem("Product", 10);

        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.ProblemDetails.Extensions.Should().ContainKey("entityId");
    }

    [Fact]
    public void ValidationProblem_Should_Create400ValidationProblem()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Required"]
        };

        IResult result = TypedResultsExtensions.ValidationProblem(errors);

        result.Should().BeOfType<ValidationProblem>();
    }
}
