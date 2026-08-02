using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Exceptions;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Exceptions;

[Trait("Category", "Unit")]
public class ExceptionMappersTest
{
    [Fact]
    public void DefaultExceptionToProblemDetailsMapper_Should_MapArgumentExceptionTo400()
    {
        var sut = new DefaultExceptionToProblemDetailsMapper(Options.Create(new MvpProblemDetailsOptions()));

        int status = sut.GetStatusCode(new ArgumentException("bad"));

        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void DefaultExceptionToProblemDetailsMapper_Should_IncludeTraceId()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.TraceIdentifier = "trace-01";
        var sut = new DefaultExceptionToProblemDetailsMapper(Options.Create(new MvpProblemDetailsOptions()));

        ProblemDetails details = sut.Map(new InvalidOperationException("error"), context);

        details.Extensions.Should().ContainKey("traceId");
    }

    [Fact]
    public void ValidationProblemDetailsMapper_Should_CreateValidationProblemDetails()
    {
        var validationError = new MessageResult("name", "required", MessageType.Error);
        var exception = new ValidationException("invalid", [validationError]);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        var sut = new ValidationProblemDetailsMapper(Options.Create(new MvpProblemDetailsOptions()));

        ProblemDetails details = sut.Map(exception, context);

        details.Should().BeOfType<ValidationProblemDetails>();
        details.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void ValidationProblemDetailsMapper_Should_ReportCanHandle()
    {
        var sut = new ValidationProblemDetailsMapper(Options.Create(new MvpProblemDetailsOptions()));

        sut.CanHandle(new ValidationException("invalid")).Should().BeTrue();
        sut.CanHandle(new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void CompositeExceptionToProblemDetailsMapper_Should_UseSpecializedMapper()
    {
        var specialized = new FakeMapper();
        var defaultMapper = new DefaultExceptionToProblemDetailsMapper(Options.Create(new MvpProblemDetailsOptions()));
        var sut = new CompositeExceptionToProblemDetailsMapper([specialized], defaultMapper);
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();

        ProblemDetails details = sut.Map(new NotSupportedException("special"), context);

        details.Title.Should().Be("custom");
        sut.GetStatusCode(new NotSupportedException()).Should().Be(418);
    }
}

internal sealed class FakeMapper : IExceptionToProblemDetailsMapper
{
    public bool CanHandle(Exception exception)
    {
        return exception is NotSupportedException;
    }

    public int GetStatusCode(Exception exception)
    {
        return 418;
    }

    public ProblemDetails Map(Exception exception, HttpContext context)
    {
        return new()
        {
            Status = 418,
            Title = "custom",
            Detail = exception.Message
        };
    }
}
