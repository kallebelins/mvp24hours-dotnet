//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Exceptions;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

[Trait("Category", "Unit")]
public class AdvancedBehaviorsTest
{
    private static ServiceProvider CreateProvider(Action<ServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IDistributedCache, MemoryDistributedCache>();
        services.AddSingleton<IIdempotencyKeyGenerator, DefaultIdempotencyKeyGenerator>();
        services.AddMvpMediator(options => options.RegisterHandlersFromAssembly(typeof(AuthorizedAdminCommand).Assembly));
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AuthorizationBehavior_Unauthenticated_ShouldThrowUnauthorized()
    {
        ServiceProvider sp = CreateProvider(services =>
        {
            services.AddSingleton<IUserContext>(new TestUserContext(isAuthenticated: false));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        });

        IMediator mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            mediator.SendAsync(new AuthorizedAdminCommand()));
    }

    [Fact]
    public async Task AuthorizationBehavior_MissingRole_ShouldThrowForbidden()
    {
        ServiceProvider sp = CreateProvider(services =>
        {
            services.AddSingleton<IUserContext>(new TestUserContext(isAuthenticated: true, "User"));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        });

        IMediator mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            mediator.SendAsync(new RestrictedCommand()));
    }

    [Fact]
    public async Task AuthorizationBehavior_ValidUser_ShouldExecuteHandler()
    {
        ServiceProvider sp = CreateProvider(services =>
        {
            services.AddSingleton<IUserContext>(new TestUserContext(isAuthenticated: true, "Admin", "User"));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        });

        IMediator mediator = sp.GetRequiredService<IMediator>();
        string result = await mediator.SendAsync(new AuthorizedAdminCommand());

        Assert.Equal("admin-ok", result);
    }

    [Fact]
    public async Task AuthorizationBehavior_NonAuthorizedRequest_ShouldPassThrough()
    {
        ServiceProvider sp = CreateProvider(services =>
        {
            services.AddSingleton<IUserContext>(new TestUserContext(isAuthenticated: false));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        });

        IMediator mediator = sp.GetRequiredService<IMediator>();
        string result = await mediator.SendAsync(new TestCommand { Name = "open", Value = 1 });

        Assert.Contains("open", result);
    }

    [Fact]
    public async Task CachingBehavior_ShouldReturnCachedResponseOnSecondCall()
    {
        CacheableTestQueryHandler.ExecutionCount = 0;

        ServiceProvider sp = CreateProvider(services =>
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>)));

        IMediator mediator = sp.GetRequiredService<IMediator>();
        var query = new CacheableTestQuery { Id = "1" };

        string first = await mediator.SendAsync(query);
        string second = await mediator.SendAsync(query);

        Assert.Equal(first, second);
        Assert.Equal(1, CacheableTestQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task CacheInvalidationBehavior_ShouldRemoveCachedEntries()
    {
        CacheableTestQueryHandler.ExecutionCount = 0;

        ServiceProvider sp = CreateProvider(services =>
        {
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));
        });

        IMediator mediator = sp.GetRequiredService<IMediator>();
        var query = new CacheableTestQuery { Id = "1" };

        string first = await mediator.SendAsync(query);
        await mediator.SendAsync(new CacheInvalidatingCommand());
        string second = await mediator.SendAsync(query);

        Assert.NotEqual(first, second);
        Assert.Equal(2, CacheableTestQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task RetryBehavior_ShouldRetryTransientFailures()
    {
        RetryTestCommand.AttemptCount = 0;

        ServiceProvider sp = CreateProvider(services =>
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>)));

        IMediator mediator = sp.GetRequiredService<IMediator>();
        string result = await mediator.SendAsync(new RetryTestCommand { FailUntilAttempt = 2 });

        Assert.Equal("retry-success", result);
        Assert.Equal(2, RetryTestCommand.AttemptCount);
    }

    [Fact]
    public void RetryPolicyExtensions_ShouldDetectDatabaseAndNetworkErrors()
    {
        var timeout = new Exception("SQL timeout while executing database command");
        var network = new HttpRequestException("network failure");

        Assert.True(timeout.IsDatabaseTimeout());
        Assert.True(network.IsNetworkError());
    }

    [Fact]
    public async Task IdempotencyBehavior_ShouldReturnCachedResultForDuplicateCommand()
    {
        IdempotentTestCommandHandler.ExecutionCount = 0;

        ServiceProvider sp = CreateProvider(services =>
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>)));

        IMediator mediator = sp.GetRequiredService<IMediator>();
        var command = new IdempotentTestCommand { OperationKey = "payment-42" };

        int first = await mediator.SendAsync(command);
        int second = await mediator.SendAsync(command);

        Assert.Equal(10, first);
        Assert.Equal(first, second);
        Assert.Equal(1, IdempotentTestCommandHandler.ExecutionCount);
    }

    [Fact]
    public void DefaultIdempotencyKeyGenerator_ShouldGenerateStableKey()
    {
        var generator = new DefaultIdempotencyKeyGenerator();
        var command = new IdempotentTestCommand { OperationKey = "x" };

        string key1 = generator.GenerateKey(command);
        string key2 = generator.GenerateKey(command);

        Assert.Equal(key1, key2);
        Assert.StartsWith("idempotency:", key1);
    }

    [Fact]
    public async Task NativeResilienceBehavior_OptInRequest_ShouldExecuteWithRetry()
    {
        FailingResilientTestQueryHandler.ExecutionCount = 0;

        ServiceProvider sp = CreateProvider(services =>
            services.AddNativeCqrsResilience(new NativeCqrsResilienceOptions
            {
                EnableRetry = true,
                RetryMaxAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(1),
                EnableCircuitBreaker = false,
                EnableTimeout = false
            }));

        IMediator mediator = sp.GetRequiredService<IMediator>();
        string result = await mediator.SendAsync(new FailingResilientTestQuery());

        Assert.Equal("recovered", result);
        Assert.Equal(2, FailingResilientTestQueryHandler.ExecutionCount);
    }

    [Fact]
    public async Task NativeResilienceBehavior_NonResilientRequest_ShouldPassThroughWhenNotGlobal()
    {
        ResilientTestQuery.ExecutionCount = 0;

        ServiceProvider sp = CreateProvider(services =>
            services.AddNativeCqrsResilience(new NativeCqrsResilienceOptions
            {
                ApplyToAllRequests = false,
                EnableRetry = true
            }));

        IMediator mediator = sp.GetRequiredService<IMediator>();
        string result = await mediator.SendAsync(new ResilientTestQuery());

        Assert.Equal("resilient-ok", result);
        Assert.Equal(1, ResilientTestQuery.ExecutionCount);
    }

    [Fact]
    public void NativeCqrsResilienceOptions_Presets_ShouldHaveExpectedDefaults()
    {
        NativeCqrsResilienceOptions commands = NativeCqrsResilienceOptions.ForCommands;
        NativeCqrsResilienceOptions queries = NativeCqrsResilienceOptions.ForQueries;

        Assert.True(commands.EnableRetry);
        Assert.True(queries.EnableRetry);
        Assert.True(commands.RetryMaxAttempts >= queries.RetryMaxAttempts);
    }
}
