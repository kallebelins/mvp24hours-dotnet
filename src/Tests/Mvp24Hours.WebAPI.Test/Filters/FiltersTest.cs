using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.ContentNegotiation;
using Mvp24Hours.WebAPI.Filters;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Filters;

[Trait("Category", "Unit")]
public class FiltersTest
{
    [Fact]
    public void ModelStateValidationFilter_Should_SetBadRequest_WhenModelInvalid()
    {
        var options = Options.Create(new MvpProblemDetailsOptions());
        var sut = new ModelStateValidationFilter(options);
        var context = WebApiTestHelpers.CreateActionExecutingContext();
        context.ModelState.AddModelError("name", "required");

        sut.OnActionExecuting(context);

        context.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void ModelStateValidationFilter_Should_KeepResultNull_WhenValid()
    {
        var sut = new ModelStateValidationFilter(Options.Create(new MvpProblemDetailsOptions()));
        var context = WebApiTestHelpers.CreateActionExecutingContext();

        sut.OnActionExecuting(context);

        context.Result.Should().BeNull();
    }

    [Fact]
    public void ProblemDetailsResultFilter_Should_ConvertStatusCodeResult()
    {
        var sut = new ProblemDetailsResultFilter(Options.Create(new MvpProblemDetailsOptions()));
        var context = WebApiTestHelpers.CreateResultExecutingContext(new StatusCodeResult(StatusCodes.Status404NotFound));

        sut.OnResultExecuting(context);

        context.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)context.Result;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        objectResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task ContentNegotiationResultFilter_Should_SetContentTypeForObjectResult()
    {
        var options = new ContentNegotiationOptions();
        var registry = new ContentFormatterRegistry(options);
        var negotiator = new AcceptHeaderNegotiator(options, registry);
        var sut = new ContentNegotiationResultFilter(negotiator, Options.Create(options), NullLogger<ContentNegotiationResultFilter>.Instance);
        var httpContext = WebApiTestHelpers.CreateHttpContext();
        httpContext.Request.Headers["Accept"] = "application/json";
        var resultExecutingContext = WebApiTestHelpers.CreateResultExecutingContext(new ObjectResult(new { ok = true }), httpContext);

        async Task<ResultExecutedContext> Next()
        {
            return await Task.FromResult(WebApiTestHelpers.CreateResultExecutedContext(resultExecutingContext.Result, httpContext));
        }

        await sut.OnResultExecutionAsync(resultExecutingContext, Next);

        var objectResult = resultExecutingContext.Result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.ContentTypes.Should().Contain(x => x.StartsWith("application/json"));
    }

    [Fact]
    public void RequireAcceptableMediaTypeAttribute_Should_Set406_WhenMediaTypeIsInvalid()
    {
        var sut = new RequireAcceptableMediaTypeAttribute("application/xml");
        var context = WebApiTestHelpers.CreateActionExecutingContext();
        context.HttpContext.Request.Headers["Accept"] = "application/json";

        sut.OnActionExecuting(context);

        context.Result.Should().BeOfType<ObjectResult>();
        ((ObjectResult)context.Result!).StatusCode.Should().Be(StatusCodes.Status406NotAcceptable);
    }

    [Fact]
    public void ProducesContentTypeAttribute_Should_StoreContentType()
    {
        var sut = new ProducesContentTypeAttribute("application/json");

        sut.ContentType.Should().Be("application/json");
        sut.IsDefault.Should().BeFalse();
    }
}
