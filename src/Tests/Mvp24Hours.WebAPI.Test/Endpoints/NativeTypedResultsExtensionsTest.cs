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
public class NativeTypedResultsExtensionsTest
{
    [Fact]
    public void ToNativeTypedResult_ShouldReturnOk_WhenSuccessfulWithData()
    {
        IBusinessResult<string> result = new BusinessResult<string>("value");

        IResult httpResult = result.ToNativeTypedResult();

        httpResult.Should().BeOfType<Ok<string>>();
    }

    [Fact]
    public void ToNativeTypedResult_ShouldReturnNotFound_WhenDataIsNull()
    {
        IBusinessResult<string> result = new BusinessResult<string>(null);

        IResult httpResult = result.ToNativeTypedResult();

        httpResult.Should().BeOfType<NotFound<ProblemDetails>>();
    }

    [Fact]
    public void ToNativeTypedResultAllowNull_ShouldReturnOk_WhenDataIsNull()
    {
        IBusinessResult<string> result = new BusinessResult<string>(null);

        IResult httpResult = result.ToNativeTypedResultAllowNull();

        httpResult.Should().BeOfType<Ok<string?>>();
    }

    [Fact]
    public void ToSimpleTypedResult_ShouldReturnBadRequest_WhenErrorsExist()
    {
        var messages = new List<IMessageResult> { new MessageResult("field", "invalid", MessageType.Error) };
        IBusinessResult<string> result = new BusinessResult<string>(null, messages);

        IResult httpResult = result.ToSimpleTypedResult();

        httpResult.Should().BeOfType<BadRequest<ProblemDetails>>();
    }

    [Fact]
    public void ToCreatedTypedResult_ShouldReturnConflict_WhenConflictErrorExists()
    {
        IBusinessResult<string> result = new BusinessResult<string>(
            null,
            [StructuredMessageResult.Conflict("Widget", "Name", "dup")]);

        IResult httpResult = result.ToCreatedTypedResult("/api/widgets/1");

        httpResult.Should().BeOfType<Conflict<ProblemDetails>>();
    }

    [Fact]
    public void ToCreatedTypedResult_ShouldReturnCreated_WhenSuccessful()
    {
        IBusinessResult<string> result = new BusinessResult<string>("id-1");

        IResult httpResult = result.ToCreatedTypedResult("/api/widgets/id-1");

        httpResult.Should().BeOfType<Created<string>>();
    }

    [Fact]
    public void ToNoContentTypedResult_ShouldReturnNotFound_WhenNotFoundErrorExists()
    {
        IBusinessResult<bool> result = new BusinessResult<bool>(
            false,
            [StructuredMessageResult.NotFound("Widget", 1)]);

        IResult httpResult = result.ToNoContentTypedResult();

        httpResult.Should().BeOfType<NotFound<ProblemDetails>>();
    }

    [Fact]
    public void ToNoContentTypedResult_ShouldReturnNoContent_WhenSuccessful()
    {
        IBusinessResult<bool> result = new BusinessResult<bool>(true);

        IResult httpResult = result.ToNoContentTypedResult();

        httpResult.Should().BeOfType<NoContent>();
    }

    [Fact]
    public void ToAcceptedTypedResult_ShouldReturnAccepted_WhenSuccessful()
    {
        IBusinessResult<string> result = new BusinessResult<string>("job-1");

        IResult httpResult = result.ToAcceptedTypedResult("/api/jobs/job-1");

        httpResult.Should().BeOfType<Accepted<string>>();
    }

    [Fact]
    public void ToNativeTypedProblem_ShouldMapNotFoundException()
    {
        ProblemHttpResult result = new NotFoundException("missing", "Item", 1).ToNativeTypedProblem(includeDetails: true);
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ToNativeTypedProblem_ShouldMapValidationException()
    {
        ProblemHttpResult result = new ValidationException("invalid", []).ToNativeTypedProblem(includeDetails: true);
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ToNativeTypedProblem_ShouldMapUnauthorizedException()
    {
        ProblemHttpResult result = new UnauthorizedException("auth").ToNativeTypedProblem(includeDetails: true);
        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void ToNativeTypedProblem_ShouldMapForbiddenException()
    {
        ProblemHttpResult result = new ForbiddenException("denied").ToNativeTypedProblem(includeDetails: true);
        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void ToNativeTypedProblem_ShouldMapConflictException()
    {
        ProblemHttpResult result = new ConflictException("conflict", "Item", "Name").ToNativeTypedProblem(includeDetails: true);
        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ToNativeTypedProblem_ShouldMapDomainException()
    {
        ProblemHttpResult result = new DomainException("rule", "Item").ToNativeTypedProblem(includeDetails: true);
        result.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    [Fact]
    public void ToNativeTypedProblemWithStackTrace_ShouldIncludeStackTrace()
    {
        var exception = new InvalidOperationException("boom");

        ProblemHttpResult result = exception.ToNativeTypedProblemWithStackTrace();

        result.ProblemDetails.Extensions.Should().ContainKey("stackTrace");
    }

    [Fact]
    public void HelperMethods_ShouldCreateTypedResults()
    {
        NativeTypedResultsExtensions.Ok("data").Should().BeOfType<Ok<string>>();
        NativeTypedResultsExtensions.Created("/a", "data").Should().BeOfType<Created<string>>();
        NativeTypedResultsExtensions.Accepted("/a", "data").Should().BeOfType<Accepted<string>>();
        NativeTypedResultsExtensions.NoContent().Should().BeOfType<NoContent>();
        NativeTypedResultsExtensions.BadRequest("bad").Should().BeOfType<BadRequest<ProblemDetails>>();
        NativeTypedResultsExtensions.Unauthorized().Should().BeOfType<UnauthorizedHttpResult>();
        NativeTypedResultsExtensions.Forbid().Should().BeOfType<ForbidHttpResult>();
        NativeTypedResultsExtensions.NotFound("Order", 1).Should().BeOfType<NotFound<ProblemDetails>>();
        NativeTypedResultsExtensions.Conflict("conflict").Should().BeOfType<Conflict<ProblemDetails>>();
        NativeTypedResultsExtensions.UnprocessableEntity("domain").Should().BeOfType<ProblemHttpResult>();
        NativeTypedResultsExtensions.InternalServerError().StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void ValidationProblem_ShouldCreateValidationProblemResult()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Required"]
        };

        ValidationProblem result = NativeTypedResultsExtensions.ValidationProblem(errors);

        result.Should().NotBeNull();
    }

    [Fact]
    public void ValidationProblem_FromTuples_ShouldGroupByProperty()
    {
        ValidationProblem result = NativeTypedResultsExtensions.ValidationProblem(
        [
            ("Name", "Required"),
            ("Name", "Too short")
        ]);

        result.Should().NotBeNull();
    }

    [Fact]
    public void Problem_ShouldIncludeExtensions()
    {
        ProblemHttpResult result = NativeTypedResultsExtensions.Problem(
            StatusCodes.Status418ImATeapot,
            "Teapot",
            "Cannot brew coffee",
            new Dictionary<string, object?> { ["reason"] = "wrong device" });

        result.StatusCode.Should().Be(StatusCodes.Status418ImATeapot);
        result.ProblemDetails.Extensions.Should().ContainKey("reason");
    }

    [Fact]
    public void ToNativeTypedResult_ShouldMapUnauthorizedAndForbiddenErrors()
    {
        IBusinessResult<string> unauthorized = new BusinessResult<string>(
            null,
            [StructuredMessageResult.Unauthorized()]);
        IBusinessResult<string> forbidden = new BusinessResult<string>(
            null,
            [StructuredMessageResult.Forbidden("Resource", "Delete")]);

        unauthorized.ToNativeTypedResult().Should().BeOfType<UnauthorizedHttpResult>();
        forbidden.ToNativeTypedResult().Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public void ToNativeTypedResult_ShouldThrow_WhenResultIsNull()
    {
        IBusinessResult<string>? result = null;

        Action act = () => result!.ToNativeTypedResult();

        act.Should().Throw<ArgumentNullException>();
    }
}
