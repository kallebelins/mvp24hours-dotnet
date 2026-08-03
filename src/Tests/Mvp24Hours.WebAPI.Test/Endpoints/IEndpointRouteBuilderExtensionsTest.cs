using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.WebAPI.Endpoints;

namespace Mvp24Hours.WebAPI.Test.Endpoints;

[Trait("Category", "Unit")]
public class IEndpointRouteBuilderExtensionsTest
{
    [Fact]
    public void MapCommand_ShouldThrow_WhenEndpointsIsNull()
    {
        IEndpointRouteBuilder? endpoints = null;
        Action act = () => endpoints!.MapCommand<CreateWidgetCommand, string>("/api/widgets");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapCommand_ShouldThrow_WhenPatternIsEmpty()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();
        Action act = () => app.MapCommand<CreateWidgetCommand, string>("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task MapCommand_ShouldReturn200_WhenCommandSucceeds()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapCommand<CreateWidgetCommand, string>("/api/widgets"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Returns<CreateWidgetCommand, string>((_, _) => Task.FromResult("created")!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync("/api/widgets", JsonContent(new CreateWidgetCommand("widget")));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("created");
        }
    }

    [Fact]
    public async Task MapCommand_ShouldReturn404_WhenNotFoundExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapCommand<CreateWidgetCommand, string>("/api/widgets"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Throws(new NotFoundException("missing", "Widget", 1)));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync("/api/widgets", JsonContent(new CreateWidgetCommand("widget")));
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task MapCommand_ShouldReturn409_WhenConflictExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapCommand<CreateWidgetCommand, string>("/api/widgets"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Throws(new ConflictException("duplicate", "Widget", "Name")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync("/api/widgets", JsonContent(new CreateWidgetCommand("widget")));
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task MapCommandWithResult_ShouldReturn400_WhenBusinessResultHasErrors()
    {
        var error = new MessageResult("name", "required", MessageType.Error);
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapCommandWithResult<CreateWidgetResultCommand, string>("/api/widgets/result"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Returns<CreateWidgetResultCommand, IBusinessResult<string>>((_, _) =>
                        Task.FromResult<IBusinessResult<string>>(new BusinessResult<string>(null, [error]))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/widgets/result",
                JsonContent(new CreateWidgetResultCommand("widget")));
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task MapQuery_ShouldReturn200_WhenQueryReturnsData()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapQuery<GetWidgetQuery, string>("/api/widgets/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Returns<GetWidgetQuery, string>((_, _) => Task.FromResult("found")!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/widgets/5");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("found");
        }
    }

    [Fact]
    public async Task MapQuery_ShouldReturn404_WhenResponseIsNull()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapQuery<GetWidgetQuery, string>("/api/widgets/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Returns<GetWidgetQuery, string>((_, _) => Task.FromResult<string?>(null)!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/widgets/5");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task MapQueryWithResult_ShouldReturn404_WhenNotFoundErrorInResult()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapQueryWithResult<GetWidgetResultQuery, string>("/api/widgets/{id}/result"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender()
                    .Returns<GetWidgetResultQuery, IBusinessResult<string>>((_, _) =>
                        Task.FromResult<IBusinessResult<string>>(
                            new BusinessResult<string>(null, [StructuredMessageResult.NotFound("Widget", 5)]))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/widgets/5/result");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task MapCommand_ShouldReturn403_WhenForbiddenExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapCommand<CreateWidgetCommand, string>("/api/widgets"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender().Throws(new ForbiddenException("denied")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync("/api/widgets", JsonContent(new CreateWidgetCommand("widget")));
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task MapQuery_ShouldReturn401_WhenUnauthorizedExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapQuery<GetWidgetQuery, string>("/api/widgets/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new EndpointStubSender().Throws(new UnauthorizedException("auth required")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/widgets/5");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public void MapCommand_Should_InvokeConfigureCallback()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();
        bool configured = false;

        RouteHandlerBuilder builder = app.MapCommand<CreateWidgetCommand, string>(
            "/api/widgets",
            configure: b => configured = b is not null);

        builder.Should().NotBeNull();
        configured.Should().BeTrue();
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateWidgetCommand>, PermissiveEndpointValidator<CreateWidgetCommand>>();
        services.AddSingleton<IValidator<CreateWidgetResultCommand>, PermissiveEndpointValidator<CreateWidgetResultCommand>>();
        services.AddSingleton<IValidator<GetWidgetQuery>, PermissiveEndpointValidator<GetWidgetQuery>>();
        services.AddSingleton<IValidator<GetWidgetResultQuery>, PermissiveEndpointValidator<GetWidgetResultQuery>>();
    }

    private static void RegisterAuthentication(IServiceCollection services)
    {
        services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, EndpointTestAuthHandler>("Test", _ => { });
        services.AddAuthorization();
    }

    private static StringContent JsonContent<T>(T payload)
    {
        string json = JsonSerializer.Serialize(payload);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    private static async Task<(HttpClient Client, IHost Host)> CreateHost(
        Action<IEndpointRouteBuilder> configureEndpoints,
        Action<IServiceCollection>? configureServices = null)
    {
        IHost host = await new HostBuilder()
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddLogging();
                    RegisterAuthentication(services);
                    configureServices?.Invoke(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(configureEndpoints);
                }))
            .StartAsync();

        return (host.GetTestClient(), host);
    }
}

internal sealed record CreateWidgetCommand(string Name) : IMediatorCommand<string>;

internal sealed record CreateWidgetResultCommand(string Name) : IMediatorCommand<IBusinessResult<string>>;

internal sealed record GetWidgetQuery(int Id) : IMediatorQuery<string>;

internal sealed record GetWidgetResultQuery(int Id) : IMediatorQuery<IBusinessResult<string>>;

internal sealed class PermissiveEndpointValidator<T> : AbstractValidator<T>;

internal sealed class EndpointTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}

internal sealed class EndpointStubSender : ISender
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = new();
    private Exception? _exception;

    public EndpointStubSender Returns<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> handler)
        where TRequest : IMediatorRequest<TResponse>
    {
        _handlers[typeof(TRequest)] = async (request, ct) => await handler((TRequest)request, ct)!;
        _exception = null;
        return this;
    }

    public EndpointStubSender Throws(Exception exception)
    {
        _exception = exception;
        _handlers.Clear();
        return this;
    }

    public async Task<TResponse> SendAsync<TResponse>(IMediatorRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        if (!_handlers.TryGetValue(request.GetType(), out Func<object, CancellationToken, Task<object?>>? handler))
        {
            throw new InvalidOperationException($"No handler configured for {request.GetType().Name}.");
        }

        return (TResponse)(await handler(request, cancellationToken))!;
    }
}
