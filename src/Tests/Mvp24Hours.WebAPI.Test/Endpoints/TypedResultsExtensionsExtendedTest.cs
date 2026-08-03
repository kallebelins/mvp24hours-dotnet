using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Moq;
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
    public void ToTypedResult_Should_ReturnOk_WhenSuccessfulWithData()
    {
        IBusinessResult<string> result = new BusinessResult<string>("value");

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<Ok<string>>();
    }

    [Fact]
    public void ToTypedResult_Should_ReturnBadRequest_WhenHasErrorsButNoMessages()
    {
        var mock = new Mock<IBusinessResult<object>>();
        mock.Setup(r => r.HasErrors).Returns(true);
        mock.Setup(r => r.Messages).Returns([]);

        IResult httpResult = mock.Object.ToTypedResult();

        httpResult.Should().BeOfType<BadRequest<ProblemDetails>>();
    }

    [Fact]
    public void ToTypedResult_Should_MapNotFoundByKey()
    {
        var messages = new List<IMessageResult>
        {
            new MessageResult("NOT_FOUND", "missing", MessageType.Error)
        };
        IBusinessResult<object> result = new BusinessResult<object>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().BeOfType<NotFound<ProblemDetails>>();
    }

    [Fact]
    public void ToProblem_Should_MapDomainAndBusinessExceptions()
    {
        ProblemHttpResult domain = new DomainException("rule", "Order", "MustBeActive").ToProblem(includeDetails: true);
        ProblemHttpResult business = new BusinessException("biz", "B001").ToProblem(includeDetails: true);
        ProblemHttpResult timeout = new TimeoutException("slow").ToProblem(includeDetails: true);

        domain.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        business.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        timeout.StatusCode.Should().Be(StatusCodes.Status408RequestTimeout);
    }

    [Fact]
    public void NotFoundProblem_WithoutEntityId_ShouldOmitEntityIdExtension()
    {
        ProblemHttpResult result = TypedResultsExtensions.NotFoundProblem("Product");

        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        result.ProblemDetails.Extensions.Should().NotContainKey("entityId");
    }

    [Fact]
    public void ConflictProblem_Should_Create409Problem()
    {
        ProblemHttpResult result = TypedResultsExtensions.ConflictProblem("duplicate", "Order", "/orders/1");

        result.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        result.ProblemDetails.Extensions["entityName"].Should().Be("Order");
    }

    [Fact]
    public void ForbiddenProblem_Should_Create403Problem()
    {
        ProblemHttpResult result = TypedResultsExtensions.ForbiddenProblem("denied", "Order", "orders:delete");

        result.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        result.ProblemDetails.Extensions["requiredPermission"].Should().Be("orders:delete");
    }

    [Fact]
    public void UnauthorizedProblem_Should_Create401Problem()
    {
        ProblemHttpResult result = TypedResultsExtensions.UnauthorizedProblem("auth required", "Bearer");

        result.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        result.ProblemDetails.Extensions["authenticationScheme"].Should().Be("Bearer");
    }

    [Fact]
    public void DomainProblem_Should_Create422Problem()
    {
        ProblemHttpResult result = TypedResultsExtensions.DomainProblem("invalid state", "Order", "MustNotShip");

        result.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        result.ProblemDetails.Extensions["ruleName"].Should().Be("MustNotShip");
    }

    [Fact]
    public void InternalServerErrorProblem_Should_Create500Problem()
    {
        ProblemHttpResult result = TypedResultsExtensions.InternalServerErrorProblem("unexpected");

        result.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        result.ProblemDetails.Detail.Should().Be("unexpected");
    }

    [Fact]
    public void CustomProblem_Should_AddExtensions()
    {
        ProblemHttpResult result = TypedResultsExtensions.CustomProblem(
            StatusCodes.Status413PayloadTooLarge,
            "Payload Too Large",
            "File too big",
            extensions: new Dictionary<string, object?> { ["maxSize"] = "10MB" });

        result.StatusCode.Should().Be(StatusCodes.Status413PayloadTooLarge);
        result.ProblemDetails.Extensions["maxSize"].Should().Be("10MB");
    }

    [Fact]
    public void ValidationProblem_WithTuples_Should_CreateValidationProblem()
    {
        var errors = new List<(string Property, string Message)>
        {
            ("Email", "Required"),
            ("Email", "Invalid")
        };

        IResult result = TypedResultsExtensions.ValidationProblem(errors, "/users");

        result.Should().BeOfType<ValidationProblem>();
    }

    [Fact]
    public void ToProblemWithStackTrace_Should_IncludeExceptionDetails()
    {
        var exception = new NotFoundException("missing", "Order", 1);

        ProblemHttpResult result = exception.ToProblemWithStackTrace("/orders/1");

        result.ProblemDetails.Extensions.Should().ContainKey("entityId");
        result.ProblemDetails.Extensions.Should().ContainKey("stackTrace");
    }

    [Fact]
    public void ToProblem_WithIncludeDetailsFalse_Should_HideUnexpectedExceptionMessage()
    {
        var exception = new Exception("sensitive internal details");

        ProblemHttpResult result = exception.ToProblem(includeDetails: false);

        result.ProblemDetails.Detail.Should().Be("An unexpected error has occurred. Please try again later.");
        result.ProblemDetails.Extensions.Should().NotContainKey("exception");
    }

    [Fact]
    public void ToProblem_WithIncludeDetailsFalse_Should_KeepSafeDomainExceptionMessage()
    {
        var exception = new DomainException("Order must be active", "Order", "MustBeActive");

        ProblemHttpResult result = exception.ToProblem(includeDetails: false);

        result.ProblemDetails.Detail.Should().Be("Order must be active");
        result.ProblemDetails.Extensions.Should().NotContainKey("exception");
    }

    [Fact]
    public void ToProblem_Should_MapOperationCanceledException()
    {
        var exception = new OperationCanceledException("request aborted");

        ProblemHttpResult result = exception.ToProblem(includeDetails: true);

        result.StatusCode.Should().Be(499);
        result.ProblemDetails.Title.Should().Be("Request Cancelled");
    }

    [Fact]
    public void ToProblem_Should_MapNotImplementedException()
    {
        var exception = new NotImplementedException("feature pending");

        ProblemHttpResult result = exception.ToProblem(includeDetails: true);

        result.StatusCode.Should().Be(StatusCodes.Status501NotImplemented);
        result.ProblemDetails.Title.Should().Be("Not Implemented");
    }

    [Fact]
    public void ToProblem_Should_MapArgumentNullException()
    {
        var exception = new ArgumentNullException("orderId");

        ProblemHttpResult result = exception.ToProblem(includeDetails: true);

        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        result.ProblemDetails.Title.Should().Be("Missing Required Value");
    }

    [Fact]
    public void ValidationProblem_WithDictionary_Should_CreateValidationProblem()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = ["Required", "Invalid format"],
            ["Name"] = ["Required"]
        };

        ValidationProblem result = TypedResultsExtensions.ValidationProblem(errors, "/users");

        result.Should().BeOfType<ValidationProblem>();
        result.ProblemDetails.Errors.Should().ContainKey("Email");
        result.ProblemDetails.Errors["Email"].Should().HaveCount(2);
        result.ProblemDetails.Instance.Should().Be("/users");
    }
}
