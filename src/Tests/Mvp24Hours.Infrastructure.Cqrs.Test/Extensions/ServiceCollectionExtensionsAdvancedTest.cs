//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Mvp24Hours.Infrastructure.Cqrs.Observability;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Extensions;

/// <summary>
/// Phase 24.4 — ServiceCollectionExtensions / MediatorOptions / extensibility DI.
/// </summary>
[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsAdvancedTest
{
    [Fact]
    public void WithObservabilityBehaviors_ShouldRegisterThreeBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithObservabilityBehaviors();
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var behaviors = sp.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();

        // Assert
        Assert.Equal(3, behaviors.Count);
        Assert.Contains(behaviors, b => b is RequestContextBehavior<TestCommand, string>);
        Assert.Contains(behaviors, b => b is TracingBehavior<TestCommand, string>);
        Assert.Contains(behaviors, b => b is TelemetryBehavior<TestCommand, string>);
    }

    [Fact]
    public void WithAuditBehavior_ShouldRegisterAuditStoreAndBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithAuditBehavior(auditAllCommands: true);
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        MediatorOptions options = sp.GetRequiredService<MediatorOptions>();
        IAuditStore store = sp.GetRequiredService<IAuditStore>();
        var behaviors = sp.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();

        // Assert
        Assert.True(options.AuditAllCommands);
        Assert.NotNull(store);
        Assert.Contains(behaviors, b => b is AuditBehavior<TestCommand, string>);
    }

    [Fact]
    public void WithExtensibility_ShouldRegisterThreeBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithExtensibility();
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var behaviors = sp.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();

        // Assert
        Assert.Equal(3, behaviors.Count);
        Assert.Contains(behaviors, b => b is PrePostProcessorBehavior<TestCommand, string>);
        Assert.Contains(behaviors, b => b is ExceptionHandlerBehavior<TestCommand, string>);
        Assert.Contains(behaviors, b => b is PipelineHookBehavior<TestCommand, string>);
    }

    [Fact]
    public void RegisterTracingBehavior_ShouldResolveTracingBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterTracingBehavior = true, typeof(TracingBehavior<TestCommand, string>));
    }

    [Fact]
    public void RegisterTelemetryBehavior_ShouldResolveTelemetryBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterTelemetryBehavior = true, typeof(TelemetryBehavior<TestCommand, string>));
    }

    [Fact]
    public void RegisterAuthorizationBehavior_ShouldResolveAuthorizationBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterAuthorizationBehavior = true, typeof(AuthorizationBehavior<TestCommand, string>));
    }

    [Fact]
    public void RegisterIdempotencyBehavior_ShouldResolveIdempotencyBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterIdempotencyBehavior = true, typeof(IdempotencyBehavior<TestCommand, string>));
    }

    [Fact]
    public void RegisterCachingBehavior_ShouldResolveCachingBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterCachingBehavior = true, typeof(CachingBehavior<TestCommand, string>));
    }

    [Fact]
    public void RegisterRetryBehavior_ShouldResolveRetryBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterRetryBehavior = true, typeof(RetryBehavior<TestCommand, string>));
    }

    [Fact]
    public void RegisterPipelineHookBehavior_ShouldResolvePipelineHookBehavior()
    {
        AssertBehaviorRegistered(o => o.RegisterPipelineHookBehavior = true, typeof(PipelineHookBehavior<TestCommand, string>));
    }

    [Fact]
    public void MediatorOptions_ShouldBeInjectableAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.PerformanceThresholdMilliseconds = 1234;
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        MediatorOptions options1 = sp.GetRequiredService<MediatorOptions>();
        MediatorOptions options2 = sp.GetRequiredService<MediatorOptions>();
        IOptions<MediatorOptions> wrapped = sp.GetRequiredService<IOptions<MediatorOptions>>();

        // Assert
        Assert.Same(options1, options2);
        Assert.Equal(1234, options1.PerformanceThresholdMilliseconds);
        Assert.Equal(1234, wrapped.Value.PerformanceThresholdMilliseconds);
    }

    [Fact]
    public void AddMediatorDecorator_WithoutMediator_ShouldThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddMediatorDecorator<NoOpMediatorDecorator>());
        Assert.Contains("IMediator", ex.Message);
    }

    [Fact]
    public void AddPreProcessor_ShouldRegisterTypedProcessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        services.AddPreProcessor<TestCommand, Phase24PreProcessor>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IPreProcessor<TestCommand> processor = sp.GetRequiredService<IPreProcessor<TestCommand>>();

        // Assert
        Assert.IsType<Phase24PreProcessor>(processor);
    }

    [Fact]
    public void AddPostProcessor_ShouldRegisterTypedProcessor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        services.AddPostProcessor<TestCommand, string, Phase24PostProcessor>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IPostProcessor<TestCommand, string> processor =
            sp.GetRequiredService<IPostProcessor<TestCommand, string>>();

        // Assert
        Assert.IsType<Phase24PostProcessor>(processor);
    }

    [Fact]
    public void AddPipelineHook_ShouldRegisterGlobalAndTypedHooks()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        services.AddPipelineHook<Phase24PipelineHook>();
        services.AddPipelineHook<TestCommand, Phase24TypedPipelineHook>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IPipelineHook global = sp.GetRequiredService<IPipelineHook>();
        IPipelineHook<TestCommand> typed = sp.GetRequiredService<IPipelineHook<TestCommand>>();

        // Assert
        Assert.IsType<Phase24PipelineHook>(global);
        Assert.IsType<Phase24TypedPipelineHook>(typed);
    }

    [Fact]
    public void AddGlobalExceptionHandler_ShouldRegisterHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        services.AddGlobalExceptionHandler<InvalidOperationException, Phase24GlobalExceptionHandler>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IExceptionHandlerGlobal<InvalidOperationException> handler =
            sp.GetRequiredService<IExceptionHandlerGlobal<InvalidOperationException>>();

        // Assert
        Assert.IsType<Phase24GlobalExceptionHandler>(handler);
    }

    [Fact]
    public void WithAdvancedResiliency_ShouldSetFlagsAndTimeout()
    {
        // Arrange
        var options = new MediatorOptions();

        // Act
        options.WithAdvancedResiliency(defaultTimeoutMs: 15000);

        // Assert
        Assert.True(options.RegisterTimeoutBehavior);
        Assert.True(options.RegisterCircuitBreakerBehavior);
        Assert.True(options.RegisterRetryBehavior);
        Assert.True(options.RegisterIdempotencyBehavior);
        Assert.Equal(15000, options.DefaultTimeoutMilliseconds);
    }

    [Fact]
    public void AddMvpInbox_WithAutomaticCleanup_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpInbox(o => o.EnableAutomaticCleanup = true);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var hosted = sp.GetServices<IHostedService>().ToList();

        // Assert
        Assert.Contains(hosted, h => h.GetType().Name.Contains("InboxCleanup"));
    }

    [Fact]
    public void WithAllBehaviors_ShouldResolveMarkerScopedBehaviorsInDocumentedOrder()
    {
        // Arrange — WithAllBehaviors() enables a subset of the 20 registerable behaviors,
        // including all 5 marker-scoped ones (Caching, Transaction, Idempotency, Retry via
        // WithAllBehaviors, plus their siblings). The registration order documented as
        // comments in ServiceCollectionExtensions.AddMvpMediator (outer -> inner) must be
        // preserved regardless of where the marker interfaces themselves are declared.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatorMemoryCache();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithAllBehaviors();
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        List<Type> resolvedOrder = sp.GetServices<IPipelineBehavior<TestCommand, string>>()
            .Select(b => b.GetType())
            .ToList();

        // Assert — expected outer -> inner order per the documented sequence for the
        // behaviors enabled by WithAllBehaviors().
        Type[] expectedOrder =
        [
            typeof(UnhandledExceptionBehavior<TestCommand, string>),
            typeof(RequestContextBehavior<TestCommand, string>),
            typeof(TracingBehavior<TestCommand, string>),
            typeof(TelemetryBehavior<TestCommand, string>),
            typeof(LoggingBehavior<TestCommand, string>),
            typeof(PerformanceBehavior<TestCommand, string>),
            typeof(AuditBehavior<TestCommand, string>),
            typeof(AuthorizationBehavior<TestCommand, string>),
            typeof(ValidationBehavior<TestCommand, string>),
            typeof(IdempotencyBehavior<TestCommand, string>),
            typeof(CachingBehavior<TestCommand, string>),
            typeof(RetryBehavior<TestCommand, string>),
            typeof(TransactionBehavior<TestCommand, string>)
        ];

        Assert.Equal(expectedOrder, resolvedOrder);
    }

    private static void AssertBehaviorRegistered(Action<MediatorOptions> configure, Type expectedBehavior)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatorMemoryCache();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            configure(options);
        });
        ServiceProvider sp = services.BuildServiceProvider();
        var behaviors = sp.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();
        Assert.Contains(behaviors, b => b.GetType() == expectedBehavior);
    }

    private sealed class NoOpMediatorDecorator(IMediator inner) : IMediatorDecorator
    {
        public IMediator InnerMediator => inner;

        public Task<TResponse> SendAsync<TResponse>(IMediatorRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return inner.SendAsync(request, cancellationToken);
        }

        public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : IMediatorNotification
        {
            return inner.PublishAsync(notification, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return inner.CreateStream(request, cancellationToken);
        }
    }

    private sealed class Phase24PreProcessor : IPreProcessor<TestCommand>
    {
        public Task ProcessAsync(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class Phase24PostProcessor : IPostProcessor<TestCommand, string>
    {
        public Task ProcessAsync(TestCommand request, string response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class Phase24PipelineHook : PipelineHookBase;

    private sealed class Phase24TypedPipelineHook : PipelineHookBase<TestCommand>;

    private sealed class Phase24GlobalExceptionHandler : IExceptionHandlerGlobal<InvalidOperationException>
    {
        public Task<ExceptionHandlingResult<object?>> HandleAsync(
            object request,
            InvalidOperationException exception,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ExceptionHandlingResult<object?>.NotHandled);
        }
    }
}
