//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mvp24Hours.WebAPI.Extensions;
using Xunit.Priority;

namespace Mvp24Hours.WebAPI.Test;

/// <summary>
/// 
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class ApplicationBuilderTest
{
    [Fact]
#pragma warning disable CS0618 // intentional: covers obsolete ExceptionMiddleware path until removal in v12
    public async Task TestExceptions1()
    {
        // arrange
        using IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddMvp24HoursWebExceptions(x => x.TraceMiddleware = false);
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMvp24HoursExceptionHandling();
                        app.UseEndpoints(endpoints => endpoints.MapGet("/", (_) => throw new System.Exception()));
                    }))
            .StartAsync();

        // act
        HttpResponseMessage response = await host.GetTestClient().GetAsync("/");

        // assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task TestExceptions2()
    {
        // arrange
        using IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddMvp24HoursWebExceptions(x =>
                        {
                            x.TraceMiddleware = false;
                            x.StatusCodeHandle = (Exception exception) => exception is NotImplementedException ? (int)HttpStatusCode.BadRequest : (int)HttpStatusCode.InternalServerError;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMvp24HoursExceptionHandling();
                        app.UseEndpoints(endpoints => endpoints.MapGet("/", requestDelegate: (_) => throw new NotImplementedException()));
                    }))
            .StartAsync();

        // act
        HttpResponseMessage response = await host.GetTestClient().GetAsync("/");

        // assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
#pragma warning restore CS0618

    [Fact]
    public async Task TestCors1()
    {
        // arrange
        using IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddMvp24HoursWebCors(x =>
                        {
                            x.AllowAll = true;
                            x.AllowRequestOptions = true;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMvp24HoursCors();
                        app.UseEndpoints(endpoints => endpoints.MapGet("/", async context => await context.Response.WriteAsync($"Running!")));
                    }))
            .StartAsync();

        // act
        HttpResponseMessage response = await host.GetTestClient().GetAsync("/");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TestCors2()
    {
        // arrange
        using IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddMvp24HoursWebCors(x =>
                        {
                            x.AllowAll = false;
                            x.AllowRequestOptions = false;
                        });
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMvp24HoursCors();
                        app.UseEndpoints(endpoints => endpoints.MapGet("/", async context =>
                            {
                                if (!context.Response.Headers.ContainsKey("Access-Control-Allow-Headers"))
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                }
                                await context.Response.WriteAsync($"Running!");
                            }));
                    }))
            .StartAsync();

        // act
        HttpResponseMessage response = await host.GetTestClient().GetAsync("/");

        // assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task TestCorrelationId1()
    {
        // arrange
        using IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                    .UseTestServer()
                    .ConfigureServices(services => services.AddRouting())
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseMvp24HoursCorrelationId();
                        app.UseEndpoints(endpoints => endpoints.MapGet("/", async context =>
                            {
                                if (context.TraceIdentifier != "123456")
                                {
                                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                                }
                                await context.Response.WriteAsync($"Running!");
                            }));
                    }))
            .StartAsync();

        // act
        HttpClient client = host.GetTestClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-ID", "123456");
        HttpResponseMessage response = await client.GetAsync("/");

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
