//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
using Xunit;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Tests for extensibility components: Pre/Post Processors, Exception Handlers, Pipeline Hooks, and Decorators.
/// </summary>
public class ExtensibilityTests
{
    #region [ Test Types ]

    public record TestCommand(string Value) : IMediatorCommand<string>;

    public class TestCommandHandler : IMediatorCommandHandler<TestCommand, string>
    {
        public Task<string> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult($"Processed: {request.Value}");
        }
    }

    public record FailingCommand(string Value) : IMediatorCommand<string>;

    public class FailingCommandHandler : IMediatorCommandHandler<FailingCommand, string>
    {
        public Task<string> Handle(FailingCommand request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Command failed");
        }
    }

    #endregion

    #region [ Pre-Processor Tests ]

    public class TestPreProcessor : IPreProcessor<TestCommand>
    {
        public static List<string> ExecutionLog { get; } = new();

        public Task ProcessAsync(TestCommand request, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"PreProcessor: {request.Value}");
            return Task.CompletedTask;
        }
    }

    public class GlobalTestPreProcessor : IPreProcessorGlobal
    {
        public static List<string> ExecutionLog { get; } = new();

        public Task ProcessAsync(object request, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"GlobalPreProcessor: {request.GetType().Name}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PreProcessor_ShouldExecuteBeforeHandler()
    {
        // Arrange
        TestPreProcessor.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPrePostProcessors();
        });
        services.AddPreProcessor<TestCommand, TestPreProcessor>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal("Processed: test", result);
        Assert.Contains("PreProcessor: test", TestPreProcessor.ExecutionLog);
    }

    [Fact]
    public async Task GlobalPreProcessor_ShouldExecuteForAllRequests()
    {
        // Arrange
        GlobalTestPreProcessor.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPrePostProcessors();
        });
        services.AddGlobalPreProcessor<GlobalTestPreProcessor>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test1"));
        await mediator.SendAsync(new TestCommand("test2"));

        // Assert
        Assert.Equal(2, GlobalTestPreProcessor.ExecutionLog.Count);
        Assert.All(GlobalTestPreProcessor.ExecutionLog, log => Assert.Contains("GlobalPreProcessor: TestCommand", log));
    }

    #endregion

    #region [ Post-Processor Tests ]

    public class TestPostProcessor : IPostProcessor<TestCommand, string>
    {
        public static List<string> ExecutionLog { get; } = new();

        public Task ProcessAsync(TestCommand request, string response, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"PostProcessor: {request.Value} -> {response}");
            return Task.CompletedTask;
        }
    }

    public class GlobalTestPostProcessor : IPostProcessorGlobal
    {
        public static List<string> ExecutionLog { get; } = new();

        public Task ProcessAsync(object request, object? response, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"GlobalPostProcessor: {request.GetType().Name} -> {response}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PostProcessor_ShouldExecuteAfterHandler()
    {
        // Arrange
        TestPostProcessor.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPrePostProcessors();
        });
        services.AddPostProcessor<TestCommand, string, TestPostProcessor>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal("Processed: test", result);
        Assert.Contains("PostProcessor: test -> Processed: test", TestPostProcessor.ExecutionLog);
    }

    [Fact]
    public async Task GlobalPostProcessor_ShouldExecuteForAllRequests()
    {
        // Arrange
        GlobalTestPostProcessor.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPrePostProcessors();
        });
        services.AddGlobalPostProcessor<GlobalTestPostProcessor>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test1"));
        await mediator.SendAsync(new TestCommand("test2"));

        // Assert
        Assert.Equal(2, GlobalTestPostProcessor.ExecutionLog.Count);
    }

    #endregion

    #region [ Exception Handler Tests ]

    public class InvalidOperationExceptionHandler 
        : IExceptionHandler<FailingCommand, string, InvalidOperationException>
    {
        public Task<ExceptionHandlingResult<string>> HandleAsync(
            FailingCommand request,
            InvalidOperationException exception,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ExceptionHandlingResult<string>.Handled($"Handled: {exception.Message}"));
        }
    }

    public class RethrowExceptionHandler 
        : IExceptionHandler<FailingCommand, string, InvalidOperationException>
    {
        public Task<ExceptionHandlingResult<string>> HandleAsync(
            FailingCommand request,
            InvalidOperationException exception,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ExceptionHandlingResult<string>.Rethrow(new ArgumentException("Replaced exception")));
        }
    }

    public class GlobalInvalidOperationExceptionHandler : IExceptionHandlerGlobal<InvalidOperationException>
    {
        public Task<ExceptionHandlingResult<object?>> HandleAsync(
            object request,
            InvalidOperationException exception,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                ExceptionHandlingResult<object?>.Handled($"Global Handled: {exception.Message}"));
        }
    }

    [Fact]
    public async Task ExceptionHandler_ShouldHandleSpecificException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithExceptionHandlers();
        });
        services.AddExceptionHandler<FailingCommand, string, InvalidOperationException, InvalidOperationExceptionHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new FailingCommand("test"));

        // Assert
        Assert.Equal("Handled: Command failed", result);
    }

    [Fact]
    public async Task ExceptionHandler_ShouldRethrowDifferentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithExceptionHandlers();
        });
        services.AddExceptionHandler<FailingCommand, string, InvalidOperationException, RethrowExceptionHandler>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            mediator.SendAsync(new FailingCommand("test")));
        Assert.Equal("Replaced exception", exception.Message);
    }

    [Fact]
    public async Task ExceptionHandler_NotHandled_ShouldPropagateOriginal()
    {
        // Arrange - no exception handler registered
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithExceptionHandlers();
        });

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            mediator.SendAsync(new FailingCommand("test")));
        Assert.Equal("Command failed", exception.Message);
    }

    #endregion

    #region [ Pipeline Hook Tests ]

    public class TestPipelineHook : PipelineHookBase
    {
        public static List<string> ExecutionLog { get; } = new();

        public override Task OnPipelineStartAsync(object request, Type requestType, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"Start: {requestType.Name}");
            return Task.CompletedTask;
        }

        public override Task OnPipelineCompleteAsync(object request, object? response, Type requestType, Type responseType, long elapsedMilliseconds, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"Complete: {requestType.Name} -> {response} ({elapsedMilliseconds}ms)");
            return Task.CompletedTask;
        }

        public override Task OnPipelineErrorAsync(object request, Exception exception, Type requestType, long elapsedMilliseconds, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"Error: {requestType.Name} -> {exception.Message}");
            return Task.CompletedTask;
        }
    }

    public class TypedTestPipelineHook : PipelineHookBase<TestCommand>
    {
        public static List<string> ExecutionLog { get; } = new();

        public override Task OnPipelineStartAsync(TestCommand request, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"TypedStart: {request.Value}");
            return Task.CompletedTask;
        }

        public override Task OnPipelineCompleteAsync(TestCommand request, object? response, long elapsedMilliseconds, CancellationToken cancellationToken)
        {
            ExecutionLog.Add($"TypedComplete: {request.Value} -> {response}");
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task PipelineHook_ShouldExecuteStartAndComplete()
    {
        // Arrange
        TestPipelineHook.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPipelineHooks();
        });
        services.AddPipelineHook<TestPipelineHook>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal(2, TestPipelineHook.ExecutionLog.Count);
        Assert.Contains("Start: TestCommand", TestPipelineHook.ExecutionLog);
        Assert.Contains(TestPipelineHook.ExecutionLog, log => log.StartsWith("Complete: TestCommand"));
    }

    [Fact]
    public async Task PipelineHook_ShouldExecuteOnError()
    {
        // Arrange
        TestPipelineHook.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPipelineHooks();
        });
        services.AddPipelineHook<TestPipelineHook>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => 
            mediator.SendAsync(new FailingCommand("test")));

        Assert.Contains("Start: FailingCommand", TestPipelineHook.ExecutionLog);
        Assert.Contains("Error: FailingCommand -> Command failed", TestPipelineHook.ExecutionLog);
    }

    [Fact]
    public async Task TypedPipelineHook_ShouldOnlyExecuteForMatchingType()
    {
        // Arrange
        TypedTestPipelineHook.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithPipelineHooks();
        });
        services.AddPipelineHook<TestCommand, TypedTestPipelineHook>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal(2, TypedTestPipelineHook.ExecutionLog.Count);
        Assert.Contains("TypedStart: test", TypedTestPipelineHook.ExecutionLog);
        Assert.Contains("TypedComplete: test -> Processed: test", TypedTestPipelineHook.ExecutionLog);
    }

    #endregion

    #region [ Mediator Decorator Tests ]

    public class TestMediatorDecorator : MediatorDecoratorBase
    {
        public static List<string> ExecutionLog { get; } = new();

        public TestMediatorDecorator(IMediator inner) : base(inner) { }

        public override async Task<TResponse> SendAsync<TResponse>(
            IMediatorRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            ExecutionLog.Add($"Before Send: {request.GetType().Name}");
            var result = await base.SendAsync(request, cancellationToken);
            ExecutionLog.Add($"After Send: {request.GetType().Name}");
            return result;
        }
    }

    public class AnotherMediatorDecorator : MediatorDecoratorBase
    {
        public static List<string> ExecutionLog { get; } = new();

        public AnotherMediatorDecorator(IMediator inner) : base(inner) { }

        public override async Task<TResponse> SendAsync<TResponse>(
            IMediatorRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            ExecutionLog.Add($"AnotherDecorator Before: {request.GetType().Name}");
            var result = await base.SendAsync(request, cancellationToken);
            ExecutionLog.Add($"AnotherDecorator After: {request.GetType().Name}");
            return result;
        }
    }

    [Fact]
    public async Task MediatorDecorator_ShouldWrapMediatorCalls()
    {
        // Arrange
        TestMediatorDecorator.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
        });
        services.AddMediatorDecorator<TestMediatorDecorator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal("Processed: test", result);
        Assert.Equal(2, TestMediatorDecorator.ExecutionLog.Count);
        Assert.Equal("Before Send: TestCommand", TestMediatorDecorator.ExecutionLog[0]);
        Assert.Equal("After Send: TestCommand", TestMediatorDecorator.ExecutionLog[1]);
    }

    [Fact]
    public async Task MediatorDecorator_MultipleDecorators_ShouldNest()
    {
        // Arrange
        TestMediatorDecorator.ExecutionLog.Clear();
        AnotherMediatorDecorator.ExecutionLog.Clear();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
        });
        services.AddMediatorDecorator<TestMediatorDecorator>();
        services.AddMediatorDecorator<AnotherMediatorDecorator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("test"));

        // Assert
        Assert.Equal("Processed: test", result);
        
        // AnotherDecorator wraps TestDecorator, so AnotherDecorator executes first
        Assert.Equal("AnotherDecorator Before: TestCommand", AnotherMediatorDecorator.ExecutionLog[0]);
        Assert.Equal("Before Send: TestCommand", TestMediatorDecorator.ExecutionLog[0]);
        Assert.Equal("After Send: TestCommand", TestMediatorDecorator.ExecutionLog[1]);
        Assert.Equal("AnotherDecorator After: TestCommand", AnotherMediatorDecorator.ExecutionLog[1]);
    }

    #endregion

    #region [ Integration Tests ]

    [Fact]
    public async Task AllExtensibilityComponents_ShouldWorkTogether()
    {
        // Arrange
        TestPreProcessor.ExecutionLog.Clear();
        TestPostProcessor.ExecutionLog.Clear();
        TestPipelineHook.ExecutionLog.Clear();
        TestMediatorDecorator.ExecutionLog.Clear();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssemblyContaining<ExtensibilityTests>();
            options.WithExtensibility();
        });
        services.AddPreProcessor<TestCommand, TestPreProcessor>();
        services.AddPostProcessor<TestCommand, string, TestPostProcessor>();
        services.AddPipelineHook<TestPipelineHook>();
        services.AddMediatorDecorator<TestMediatorDecorator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.SendAsync(new TestCommand("integration"));

        // Assert
        Assert.Equal("Processed: integration", result);
        
        // Verify all components executed
        Assert.Single(TestPreProcessor.ExecutionLog);
        Assert.Single(TestPostProcessor.ExecutionLog);
        Assert.Equal(2, TestPipelineHook.ExecutionLog.Count); // Start + Complete
        Assert.Equal(2, TestMediatorDecorator.ExecutionLog.Count); // Before + After
    }

    [Fact]
    public void ExceptionHandlingResult_ShouldCreateCorrectStates()
    {
        // Handled
        var handled = ExceptionHandlingResult<string>.Handled("test");
        Assert.True(handled.IsHandled);
        Assert.False(handled.ShouldRethrow);
        Assert.Equal("test", handled.Response);
        Assert.Null(handled.ExceptionToRethrow);

        // NotHandled
        var notHandled = ExceptionHandlingResult<string>.NotHandled;
        Assert.False(notHandled.IsHandled);
        Assert.False(notHandled.ShouldRethrow);
        Assert.Null(notHandled.Response);
        Assert.Null(notHandled.ExceptionToRethrow);

        // Rethrow
        var exception = new InvalidOperationException("test");
        var rethrow = ExceptionHandlingResult<string>.Rethrow(exception);
        Assert.False(rethrow.IsHandled);
        Assert.True(rethrow.ShouldRethrow);
        Assert.Null(rethrow.Response);
        Assert.Same(exception, rethrow.ExceptionToRethrow);
    }

    #endregion
}

