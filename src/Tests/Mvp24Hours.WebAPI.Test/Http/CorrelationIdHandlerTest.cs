using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mvp24Hours.WebAPI.Configuration;
using Mvp24Hours.WebAPI.Http;
using Mvp24Hours.WebAPI.Middlewares;
using Mvp24Hours.WebAPI.Test.Support;

namespace Mvp24Hours.WebAPI.Test.Http;

[Trait("Category", "Unit")]
public class CorrelationIdHandlerTest
{
    [Fact]
    public async Task CorrelationIdHandler_Should_PropagateHeaders()
    {
        DefaultHttpContext context = WebApiTestHelpers.CreateHttpContext();
        context.Items[RequestContextKeys.CorrelationId] = "corr-1";
        context.Items[RequestContextKeys.RequestId] = "req-1";
        context.Items[RequestContextKeys.TenantId] = "tenant-1";
        var accessor = new HttpContextAccessor { HttpContext = context };
        IOptions<RequestContextOptions> options = Options.Create(new RequestContextOptions());
        var handler = new CorrelationIdHandler(accessor, options)
        {
            InnerHandler = new StubDelegatingHandler(request =>
            {
                request.Headers.Contains(options.Value.CorrelationIdHeader).Should().BeTrue();
                request.Headers.Contains(options.Value.CausationIdHeader).Should().BeTrue();
                request.Headers.Contains(options.Value.TenantIdHeader).Should().BeTrue();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            })
        };
        var client = new HttpClient(handler);

        HttpResponseMessage response = await client.GetAsync("https://example.test/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CorrelationIdPropagatingHandler_Should_UseProviderContext()
    {
        var provider = new AsyncLocalCorrelationContextProvider();
        provider.SetCurrentContext(CorrelationContext.Create(correlationId: "corr-2", tenantId: "tenant-2"));
        IOptions<RequestContextOptions> options = Options.Create(new RequestContextOptions());
        var handler = new CorrelationIdPropagatingHandler(provider, options)
        {
            InnerHandler = new StubDelegatingHandler(request =>
            {
                request.Headers.Contains(options.Value.CorrelationIdHeader).Should().BeTrue();
                request.Headers.Contains(options.Value.TenantIdHeader).Should().BeTrue();
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
            })
        };
        var client = new HttpClient(handler);

        HttpResponseMessage response = await client.GetAsync("https://example.test/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
