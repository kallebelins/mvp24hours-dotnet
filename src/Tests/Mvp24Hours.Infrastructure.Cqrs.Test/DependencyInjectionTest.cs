//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Reflection;
using FluentValidation;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Tests for DI container integration.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
[Trait("Category", "Unit")]
public class DependencyInjectionTest
{
    [Fact, Priority(1)]
    public void AddMvpMediator_ShouldRegisterIMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Assert
        IMediator? mediator = sp.GetRequiredService<IMediator>();
        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }

    [Fact, Priority(2)]
    public void AddMvpMediator_ShouldRegisterISender()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        ISender? sender = sp.GetRequiredService<ISender>();

        // Assert
        Assert.NotNull(sender);
    }

    [Fact, Priority(3)]
    public void AddMvpMediator_ShouldRegisterIPublisher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IPublisher? publisher = sp.GetRequiredService<IPublisher>();

        // Assert
        Assert.NotNull(publisher);
    }

    [Fact, Priority(4)]
    public void AddMvpMediator_ShouldRegisterIStreamSender()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IStreamSender? streamSender = sp.GetRequiredService<IStreamSender>();

        // Assert
        Assert.NotNull(streamSender);
    }

    [Fact, Priority(5)]
    public void AddMvpMediator_ShouldRegisterIDomainEventDispatcher()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IDomainEventDispatcher? dispatcher = sp.GetRequiredService<IDomainEventDispatcher>();

        // Assert
        Assert.NotNull(dispatcher);
        Assert.IsType<DomainEventDispatcher>(dispatcher);
    }

    [Fact, Priority(6)]
    public void AddMvpMediator_ShouldRegisterHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IMediatorRequestHandler<TestCommand, string>? handler = sp.GetRequiredService<IMediatorRequestHandler<TestCommand, string>>();

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<TestCommandHandler>(handler);
    }

    [Fact, Priority(7)]
    public void AddMvpMediator_ShouldRegisterNotificationHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(OrderCreatedNotification).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var handlers = sp.GetServices<IMediatorNotificationHandler<OrderCreatedNotification>>().ToList();

        // Assert
        Assert.Equal(2, handlers.Count); // EmailHandler and AuditHandler
    }

    [Fact, Priority(8)]
    public void AddMvpMediator_WithDefaultBehaviors_ShouldRegisterBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.WithDefaultBehaviors();
        });
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var behaviors = sp.GetServices<IPipelineBehavior<TestCommand, string>>().ToList();

        // Assert
        Assert.Equal(3, behaviors.Count); // UnhandledException, Logging, Performance
    }

    [Fact, Priority(9)]
    public void AddMvpMediator_WithValidationBehavior_ShouldRegisterValidationBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(CreateUserCommand).Assembly);
            options.RegisterValidationBehavior = true;
        });
        services.AddTransient<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        var behaviors = sp.GetServices<IPipelineBehavior<CreateUserCommand, int>>().ToList();

        // Assert
        Assert.Single(behaviors);
        Assert.IsType<ValidationBehavior<CreateUserCommand, int>>(behaviors[0]);
    }

    [Fact, Priority(10)]
    public void AddMvpMediator_WithMultipleAssemblies_ShouldRegisterFromAll()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        Assembly assembly = typeof(TestCommand).Assembly;
        services.AddMvpMediator(assembly); // Passing same assembly multiple times for test
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IMediatorRequestHandler<TestCommand, string>? commandHandler = sp.GetRequiredService<IMediatorRequestHandler<TestCommand, string>>();
        IMediatorRequestHandler<GetUserQuery, UserDto?>? queryHandler = sp.GetRequiredService<IMediatorRequestHandler<GetUserQuery, UserDto?>>();

        // Assert
        Assert.NotNull(commandHandler);
        Assert.NotNull(queryHandler);
    }

    [Fact, Priority(11)]
    public void MediatorOptions_WithAllBehaviors_ShouldSetAllFlags()
    {
        // Arrange
        var options = new MediatorOptions();

        // Act
        options.WithAllBehaviors();

        // Assert
        Assert.True(options.RegisterLoggingBehavior);
        Assert.True(options.RegisterPerformanceBehavior);
        Assert.True(options.RegisterUnhandledExceptionBehavior);
        Assert.True(options.RegisterValidationBehavior);
        Assert.True(options.RegisterCachingBehavior);
        Assert.True(options.RegisterTransactionBehavior);
        Assert.True(options.RegisterAuthorizationBehavior);
        Assert.True(options.RegisterRetryBehavior);
        Assert.True(options.RegisterIdempotencyBehavior);
    }

    [Fact, Priority(12)]
    public void MediatorOptions_WithPipelineCompatibility_ShouldSetBreakOnFail()
    {
        // Arrange
        var options = new MediatorOptions();

        // Act
        options.WithPipelineCompatibility();

        // Assert
        Assert.True(options.IsBreakOnFail);
        Assert.True(options.ForceRollbackOnFailure);
        Assert.True(options.RegisterTransactionBehavior);
    }

    [Fact, Priority(13)]
    public void MediatorOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new MediatorOptions();

        // Assert
        Assert.Equal(500, options.PerformanceThresholdMilliseconds);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(100, options.RetryBaseDelayMilliseconds);
        Assert.Equal(24, options.IdempotencyDurationHours);
        Assert.Equal(NotificationPublishingStrategy.Sequential, options.DefaultNotificationStrategy);
    }

    [Fact, Priority(14)]
    public void IMediator_ISender_IPublisher_ShouldBeSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        using IServiceScope scope = sp.CreateScope();
        IServiceProvider scopedSp = scope.ServiceProvider;

        // Act
        IMediator mediator = scopedSp.GetRequiredService<IMediator>();
        ISender sender = scopedSp.GetRequiredService<ISender>();
        IPublisher publisher = scopedSp.GetRequiredService<IPublisher>();
        IStreamSender streamSender = scopedSp.GetRequiredService<IStreamSender>();

        // Assert - All should be the same scoped instance
        Assert.Same(mediator, sender);
        Assert.Same(mediator, publisher);
        Assert.Same(mediator, streamSender);
    }

    [Fact, Priority(15)]
    public void AddMvpMediator_WithSecurityBehaviors_ShouldRegisterValidationAndAuthorization()
    {
        // Arrange
        var options = new MediatorOptions();

        // Act
        options.WithSecurityBehaviors();

        // Assert
        Assert.True(options.RegisterValidationBehavior);
        Assert.True(options.RegisterAuthorizationBehavior);
    }

    [Fact, Priority(16)]
    public void AddMvpMediator_WithResiliencyBehaviors_ShouldRegisterRetryAndIdempotency()
    {
        // Arrange
        var options = new MediatorOptions();

        // Act
        options.WithResiliencyBehaviors();

        // Assert
        Assert.True(options.RegisterRetryBehavior);
        Assert.True(options.RegisterIdempotencyBehavior);
    }

    [Fact, Priority(17)]
    public async Task MediatorOptions_RegisterHandlersFromAssemblyContaining_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<TestCommand>();
        });
        ServiceProvider sp = services.BuildServiceProvider();
        IMediator mediator = sp.GetRequiredService<IMediator>();
        var command = new TestCommand { Name = "AssemblyContainingTest", Value = 99 };

        // Act
        var result = await mediator.SendAsync(command);

        // Assert
        Assert.Contains("AssemblyContainingTest", result);
    }
}

