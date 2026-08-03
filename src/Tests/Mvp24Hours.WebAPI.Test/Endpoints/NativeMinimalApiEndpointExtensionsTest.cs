using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
public class NativeMinimalApiEndpointExtensionsTest
{
    [Fact]
    public void MapNativeCommand_Should_Throw_WhenEndpointsIsNull()
    {
        IEndpointRouteBuilder? endpoints = null;

        Action act = () => endpoints!.MapNativeCommand<CreateItemCommand, string>("/api/items");

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapNativeCommand_Should_Throw_WhenPatternIsEmpty()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();

        Action act = () => app.MapNativeCommand<CreateItemCommand, string>("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return200_WhenCommandSucceeds()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<CreateItemCommand, string>((_, _) => Task.FromResult("created")!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("created");
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return404_WhenNotFoundExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new NotFoundException("missing", "Item", 42)));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return400_WhenValidationExceptionThrown()
    {
        var validationError = new MessageResult("name", "required", MessageType.Error);
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new Core.Exceptions.ValidationException("invalid", [validationError])));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return409_WhenConflictExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new ConflictException("duplicate", "Item", "Name")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return401_WhenUnauthorizedExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new UnauthorizedException("auth required")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return403_WhenForbiddenExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
                services.AddAuthorization();
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new ForbiddenException("denied")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return422_WhenDomainExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new DomainException("rule violated", "Item")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be((HttpStatusCode)422);
        }
    }

    [Fact]
    public async Task MapNativeCommand_Should_Return500_WhenUnexpectedExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommand<CreateItemCommand, string>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new InvalidOperationException("unexpected")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items",
                JsonContent(new CreateItemCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }

    [Fact]
    public async Task MapNativeCommandWithResult_Should_Return200_WhenBusinessResultSucceeds()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommandWithResult<CreateItemResultCommand, string>("/api/items/result"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<CreateItemResultCommand, IBusinessResult<string>>((_, _) =>
                        Task.FromResult<IBusinessResult<string>>(new BusinessResult<string>("ok"))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items/result",
                JsonContent(new CreateItemResultCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task MapNativeCommandCreate_Should_Return201_WhenSuccessful()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommandCreate<CreateItemWithDtoCommand, CreatedItemDto>(
                "/api/items/create",
                "/api/items/create/{0}",
                dto => dto.Id),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<CreateItemWithDtoCommand, IBusinessResult<CreatedItemDto>>((_, _) =>
                        Task.FromResult<IBusinessResult<CreatedItemDto>>(new BusinessResult<CreatedItemDto>(new CreatedItemDto(99, "new")))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items/create",
                JsonContent(new CreateItemResultCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location?.ToString().Should().Contain("99");
        }
    }

    [Fact]
    public async Task MapNativeCommandDelete_Should_Return204_WhenSuccessful()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommandDelete<DeleteItemCommand, bool>("/api/items/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<DeleteItemCommand, IBusinessResult<bool>>((_, _) =>
                        Task.FromResult<IBusinessResult<bool>>(new BusinessResult<bool>(true))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.DeleteAsync("/api/items/10");

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public async Task MapNativeQuery_Should_Return200_WhenQueryReturnsData()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQuery<GetItemQuery, string>("/api/items/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<GetItemQuery, string>((query, _) => Task.FromResult($"item-{query.Id}")!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items/5");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("item-5");
        }
    }

    [Fact]
    public async Task MapNativeQuery_Should_Return404_WhenQueryReturnsNull()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQuery<GetItemQuery, string>("/api/items/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<GetItemQuery, string>((_, _) => Task.FromResult<string?>(null)!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items/5");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task MapNativeQueryWithResult_Should_Return200_WhenBusinessResultHasData()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQueryWithResult<GetItemResultQuery, string>("/api/items/{id}/result"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<GetItemResultQuery, IBusinessResult<string>>((query, _) =>
                        Task.FromResult<IBusinessResult<string>>(new BusinessResult<string>($"item-{query.Id}"))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items/7/result");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task MapNativeQueryList_Should_Return200_ForCollection()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQueryList<ListItemsQuery, string[]>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<ListItemsQuery, string[]>((_, _) => Task.FromResult(new[] { "a", "b" })!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Contain("a");
        }
    }

    [Fact]
    public void MapNativeCommand_Should_InvokeConfigureCallback()
    {
        WebApplication app = WebApplication.CreateBuilder().Build();
        bool configured = false;

        RouteHandlerBuilder builder = app.MapNativeCommand<CreateItemCommand, string>(
            "/api/items",
            configure: b => configured = b is not null);

        builder.Should().NotBeNull();
        configured.Should().BeTrue();
    }

    [Fact]
    public async Task MapNativeCommandWithResult_Should_Return404_WhenBusinessResultHasNotFoundError()
    {
        var notFoundMessage = StructuredMessageResult.NotFound("Item", 99);
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommandWithResult<CreateItemResultCommand, string>("/api/items/result"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<CreateItemResultCommand, IBusinessResult<string>>((_, _) =>
                        Task.FromResult<IBusinessResult<string>>(
                            new BusinessResult<string>(null, [notFoundMessage]))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items/result",
                JsonContent(new CreateItemResultCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task MapNativeCommandCreate_Should_Return400_WhenBusinessResultHasErrors()
    {
        var validationError = new MessageResult("name", "required", MessageType.Error);
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommandCreate<CreateItemWithDtoCommand, CreatedItemDto>(
                "/api/items/create",
                "/api/items/create/{0}",
                dto => dto.Id),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Returns<CreateItemWithDtoCommand, IBusinessResult<CreatedItemDto>>((_, _) =>
                        Task.FromResult<IBusinessResult<CreatedItemDto>>(
                            new BusinessResult<CreatedItemDto>(null, [validationError]))!));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/api/items/create",
                JsonContent(new CreateItemResultCommand("widget")));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task MapNativeQuery_Should_Return401_WhenUnauthorizedExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQuery<GetItemQuery, string>("/api/items/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new UnauthorizedException("auth required")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items/5");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task MapNativeQuery_Should_Return422_WhenDomainExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQuery<GetItemQuery, string>("/api/items/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new DomainException("rule violated", "Item")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items/5");
            response.StatusCode.Should().Be((HttpStatusCode)422);
        }
    }

    [Fact]
    public async Task MapNativeQueryList_Should_Return401_WhenUnauthorizedExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeQueryList<ListItemsQuery, string[]>("/api/items"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new UnauthorizedException("auth required")));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.GetAsync("/api/items");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task MapNativeCommandDelete_Should_Return404_WhenNotFoundExceptionThrown()
    {
        (HttpClient client, IHost host) = await CreateHost(
            endpoints => endpoints.MapNativeCommandDelete<DeleteItemCommand, bool>("/api/items/{id}"),
            services =>
            {
                RegisterValidators(services);
                services.AddSingleton<ISender>(new StubSender()
                    .Throws(new NotFoundException("missing", "Item", 10)));
            });

        using (host)
        using (client)
        {
            HttpResponseMessage response = await client.DeleteAsync("/api/items/10");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public void MapNativeQuery_Should_Throw_WhenEndpointsIsNull()
    {
        IEndpointRouteBuilder? endpoints = null;
        Action act = () => endpoints!.MapNativeQuery<GetItemQuery, string>("/api/items/{id}");
        act.Should().Throw<ArgumentNullException>();
    }

    private static void RegisterValidators(IServiceCollection services)
    {
        services.AddSingleton<IValidator<CreateItemCommand>, PermissiveValidator<CreateItemCommand>>();
        services.AddSingleton<IValidator<CreateItemResultCommand>, PermissiveValidator<CreateItemResultCommand>>();
        services.AddSingleton<IValidator<CreateItemWithDtoCommand>, PermissiveValidator<CreateItemWithDtoCommand>>();
        services.AddSingleton<IValidator<DeleteItemCommand>, PermissiveValidator<DeleteItemCommand>>();
        services.AddSingleton<IValidator<GetItemQuery>, PermissiveValidator<GetItemQuery>>();
        services.AddSingleton<IValidator<GetItemResultQuery>, PermissiveValidator<GetItemResultQuery>>();
        services.AddSingleton<IValidator<ListItemsQuery>, PermissiveValidator<ListItemsQuery>>();
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
                    configureServices?.Invoke(services);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(configureEndpoints);
                }))
            .StartAsync();

        return (host.GetTestClient(), host);
    }
}

internal sealed record CreateItemCommand(string Name) : IMediatorCommand<string>;

internal sealed record CreateItemResultCommand(string Name) : IMediatorCommand<IBusinessResult<string>>;

internal sealed record CreateItemWithDtoCommand(string Name) : IMediatorCommand<IBusinessResult<CreatedItemDto>>;

internal sealed record DeleteItemCommand(int Id) : IMediatorCommand<IBusinessResult<bool>>;

internal sealed record GetItemQuery(int Id) : IMediatorQuery<string>;

internal sealed record GetItemResultQuery(int Id) : IMediatorQuery<IBusinessResult<string>>;

internal sealed record ListItemsQuery : IMediatorQuery<string[]>;

internal sealed record CreatedItemDto(int Id, string Name);

internal sealed class PermissiveValidator<T> : AbstractValidator<T>;

internal sealed class StubSender : ISender
{
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = [];
    private Exception? _exception;

    public StubSender Returns<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> handler)
        where TRequest : IMediatorRequest<TResponse>
    {
        _handlers[typeof(TRequest)] = async (request, ct) => await handler((TRequest)request, ct)!;
        _exception = null;
        return this;
    }

    public StubSender Throws(Exception exception)
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

        object? result = await handler(request, cancellationToken);
        return (TResponse)result!;
    }
}

internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
