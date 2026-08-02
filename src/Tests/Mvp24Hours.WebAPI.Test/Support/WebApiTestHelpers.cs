using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Mvp24Hours.WebAPI.Test.Support;

internal static class WebApiTestHelpers
{
    public static DefaultHttpContext CreateHttpContext(
        string method = "GET",
        string path = "/",
        string? body = null,
        string? contentType = "application/json")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        if (body is not null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.ContentType = contentType;
        }

        return context;
    }

    public static IOptions<T> CreateOptions<T>(T value) where T : class
    {
        return Options.Create(value);
    }

    public static ILogger<T> CreateNullLogger<T>()
    {
        return NullLogger<T>.Instance;
    }

    public static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        string content = await reader.ReadToEndAsync();
        context.Response.Body.Position = 0;
        return content;
    }

    public static async Task ExecuteMiddlewareAsync(
        Func<HttpContext, Task> middleware,
        HttpContext context)
    {
        await middleware(context);
        await context.Response.CompleteAsync();
    }

    public static ModelBindingContext CreateModelBindingContext(
        string modelName,
        string? rawValue = null,
        Type? modelType = null)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        ModelMetadata metadata = metadataProvider.GetMetadataForType(modelType ?? typeof(string));

        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            [modelName] = rawValue
        });
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            query,
            System.Globalization.CultureInfo.InvariantCulture);

        var actionContext = new ActionContext(
            CreateHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            new CompositeValueProvider { valueProvider },
            metadata,
            bindingInfo: null,
            modelName: modelName);
    }

    public static ActionExecutingContext CreateActionExecutingContext(HttpContext? httpContext = null)
    {
        var context = new ActionContext(
            httpContext ?? CreateHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return new ActionExecutingContext(
            context,
            [],
            new Dictionary<string, object?>(),
            controller: new object());
    }

    public static ResultExecutingContext CreateResultExecutingContext(
        IActionResult result,
        HttpContext? httpContext = null)
    {
        var actionContext = new ActionContext(
            httpContext ?? CreateHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutingContext(
            actionContext,
            [],
            result,
            controller: new object());
    }

    public static ResultExecutedContext CreateResultExecutedContext(
        IActionResult result,
        HttpContext? httpContext = null)
    {
        var actionContext = new ActionContext(
            httpContext ?? CreateHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ResultExecutedContext(
            actionContext,
            [],
            result,
            controller: new object());
    }

    public static IDistributedCache CreateMemoryDistributedCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        return services.BuildServiceProvider().GetRequiredService<IDistributedCache>();
    }

    public static IServiceProvider CreateServiceProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public static HttpClient CreateHttpClient(HttpStatusCode statusCode = HttpStatusCode.OK, string content = "ok")
    {
        return new HttpClient(new StubDelegatingHandler(_ =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            })));
    }

    public static EndpointFilterInvocationContext CreateEndpointFilterContext(
        HttpContext httpContext,
        params object?[] arguments)
    {
        return new TestEndpointFilterInvocationContext(httpContext, arguments);
    }

    public static ModelBindingContext CreatePagingModelBindingContext(
        string modelName = "paging",
        string? limit = null,
        string? offset = null,
        string? pageSize = null,
        string? page = null,
        string? orderBy = null,
        string? navigation = null,
        Type? modelType = null)
    {
        var queryDict = new Dictionary<string, StringValues>();

        if (limit != null)
        {
            queryDict["limit"] = limit;
        }

        if (offset != null)
        {
            queryDict["offset"] = offset;
        }

        if (pageSize != null)
        {
            queryDict["pageSize"] = pageSize;
        }

        if (page != null)
        {
            queryDict["page"] = page;
        }

        if (orderBy != null)
        {
            queryDict["orderBy"] = orderBy;
        }

        if (navigation != null)
        {
            queryDict["navigation"] = navigation;
        }

        var metadataProvider = new EmptyModelMetadataProvider();
        ModelMetadata metadata = metadataProvider.GetMetadataForType(modelType ?? typeof(Mvp24Hours.Core.ValueObjects.Logic.PagingCriteria));

        var query = new QueryCollection(queryDict);
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            query,
            System.Globalization.CultureInfo.InvariantCulture);

        var actionContext = new ActionContext(
            CreateHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());

        return DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            new CompositeValueProvider { valueProvider },
            metadata,
            bindingInfo: null,
            modelName: modelName);
    }
}

internal sealed class StubDelegatingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return handler(request);
    }
}

internal sealed class TestEndpointFilterInvocationContext(HttpContext httpContext, params object?[] args)
    : EndpointFilterInvocationContext
{
    private readonly IList<object?> _arguments = [.. args];

    public override HttpContext HttpContext { get; } = httpContext;

    public override IList<object?> Arguments => _arguments;

    public override T GetArgument<T>(int index)
    {
        return (T)_arguments[index]!;
    }
}
