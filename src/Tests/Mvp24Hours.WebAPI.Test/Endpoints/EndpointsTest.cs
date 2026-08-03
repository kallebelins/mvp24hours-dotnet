using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.WebAPI.Endpoints;
using Mvp24Hours.WebAPI.Endpoints.Filters;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Endpoints;

[Trait("Category", "Unit")]
public class EndpointsTest
{
    [Fact]
    public async Task ValidationEndpointFilter_Should_ReturnValidationProblem_WhenInvalid()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CreateOrderRequest>, FailingOrderValidator>();
        HttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.RequestServices = services.BuildServiceProvider();
        EndpointFilterInvocationContext invocation = WebApiTestHelpers.CreateEndpointFilterContext(context, new CreateOrderRequest(""));
        var sut = new ValidationEndpointFilter<CreateOrderRequest>();

        object? result = await sut.InvokeAsync(invocation, _ => ValueTask.FromResult<object?>("ok"));

        result.Should().BeOfType<ProblemHttpResult>();
    }

    [Fact]
    public async Task ValidationEndpointFilter_Should_CallNext_WhenValid()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<CreateOrderRequest>, PassingOrderValidator>();
        HttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.RequestServices = services.BuildServiceProvider();
        EndpointFilterInvocationContext invocation = WebApiTestHelpers.CreateEndpointFilterContext(context, new CreateOrderRequest("ok"));
        var sut = new ValidationEndpointFilter<CreateOrderRequest>();

        object? result = await sut.InvokeAsync(invocation, _ => ValueTask.FromResult<object?>("next-called"));

        result.Should().Be("next-called");
    }

    [Fact]
    public void TypedResultsExtensions_Should_ReturnOk_WhenBusinessResultHasData()
    {
        IBusinessResult<string> result = new BusinessResult<string>("done");

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void TypedResultsExtensions_Should_ReturnBadRequest_WhenBusinessResultHasErrors()
    {
        var messages = new List<Mvp24Hours.Core.Contract.ValueObjects.Logic.IMessageResult>
        {
            new MessageResult("VALIDATION", "invalid", MessageType.Error)
        };
        IBusinessResult<string> result = new BusinessResult<string>(null, messages);

        IResult httpResult = result.ToTypedResult();

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void NativeTypedResultsExtensions_Should_ReturnCreated_ForSuccessfulResult()
    {
        IBusinessResult<string> result = new BusinessResult<string>("id-1");

        IResult httpResult = result.ToCreatedTypedResult("/api/orders/1");

        httpResult.Should().NotBeNull();
    }

    [Fact]
    public void NativeTypedResultsExtensions_Should_CreateNotFoundProblem()
    {
        NotFound<Microsoft.AspNetCore.Mvc.ProblemDetails> result = NativeTypedResultsExtensions.NotFound("Order", 10);

        result.Should().NotBeNull();
    }

    [Fact]
    public void EndpointGroupExtensions_Should_CreateApiGroup()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        RouteGroupBuilder group = app.MapMvpApiGroup("/api/orders", "Orders", requireAuthorization: false);

        group.Should().NotBeNull();
    }

    [Fact]
    public void EndpointGroupExtensions_Should_Throw_WhenPrefixIsEmpty()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        Action act = () => app.MapMvpGroup("", null);

        act.Should().Throw<ArgumentException>();
    }
}

internal sealed record CreateOrderRequest(string Name);

internal sealed class FailingOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public FailingOrderValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

internal sealed class PassingOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public PassingOrderValidator()
    {
        RuleFor(x => x.Name).Must(_ => true);
    }
}
