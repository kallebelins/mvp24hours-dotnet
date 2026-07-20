//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Reflection;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Handlers;

/// <summary>
/// Phase 24.1 — handler registration and Mediator send/stream edge cases.
/// Maps planned Handlers/* to real IMediatorRequestHandler scanning + Mediator wrappers.
/// </summary>
[Trait("Category", "Unit")]
public class MediatorHandlerRegistrationTest
{
    [Fact]
    public void AddMvpMediator_ShouldRegisterStreamRequestHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(GetItemsStreamRequest).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IStreamRequestHandler<GetItemsStreamRequest, int>? handler =
            sp.GetRequiredService<IStreamRequestHandler<GetItemsStreamRequest, int>>();

        // Assert
        Assert.NotNull(handler);
        Assert.IsType<GetItemsStreamHandler>(handler);
    }

    [Fact]
    public void AddMvpMediator_ShouldRegisterCommandHandlerAsRequestHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IMediatorRequestHandler<TestCommand, string> asRequest =
            sp.GetRequiredService<IMediatorRequestHandler<TestCommand, string>>();
        IMediatorCommandHandler<TestCommand, string>? asCommand =
            asRequest as IMediatorCommandHandler<TestCommand, string>;

        // Assert
        Assert.NotNull(asRequest);
        Assert.NotNull(asCommand);
        Assert.IsType<TestCommandHandler>(asRequest);
    }

    [Fact]
    public void AddMvpMediator_WithParamsAssemblies_ShouldScanAllAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        Assembly assembly = typeof(TestCommand).Assembly;
        services.AddMvpMediator(assembly, assembly);
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IMediatorRequestHandler<TestCommand, string> commandHandler =
            sp.GetRequiredService<IMediatorRequestHandler<TestCommand, string>>();
        IStreamRequestHandler<GetItemsStreamRequest, int> streamHandler =
            sp.GetRequiredService<IStreamRequestHandler<GetItemsStreamRequest, int>>();

        // Assert
        Assert.NotNull(commandHandler);
        Assert.NotNull(streamHandler);
    }

    [Fact]
    public void AddMvpMediator_WithoutAssemblies_ShouldStillResolveMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator();
        ServiceProvider sp = services.BuildServiceProvider();

        // Act
        IMediator mediator = sp.GetRequiredService<IMediator>();

        // Assert
        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }

    [Fact]
    public async Task CreateStream_WhenHandlerMissing_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(); // no handlers
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (int _ in mediator.CreateStream(new UnhandledStreamRequest()))
            {
            }
        });

        // Assert
        Assert.Contains("Stream handler not found", ex.Message);
        Assert.Contains(nameof(UnhandledStreamRequest), ex.Message);
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerThrows_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator();
        services.AddTransient<IMediatorNotificationHandler<FailingDispatchEvent>, ThrowingNotificationHandler>();
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act & Assert
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.PublishAsync(new FailingDispatchEvent { Reason = "boom" }));
        Assert.Equal("Handler boom", ex.Message);
    }

    [Fact]
    public async Task PublishAsync_WhenFirstHandlerThrows_ShouldNotInvokeSubsequentHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator();
        services.AddTransient<IMediatorNotificationHandler<FailingDispatchEvent>, ThrowingNotificationHandler>();
        services.AddTransient<IMediatorNotificationHandler<FailingDispatchEvent>, RecordingNotificationHandler>();
        RecordingNotificationHandler.Handled.Clear();
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.PublishAsync(new FailingDispatchEvent()));

        // Assert
        Assert.Empty(RecordingNotificationHandler.Handled);
    }

    [Fact]
    public async Task SendAsync_WithEmptyPipeline_ShouldInvokeHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(TestCommand).Assembly);
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        string result = await mediator.SendAsync(new TestCommand { Name = "Direct", Value = 7 });

        // Assert
        Assert.Contains("Direct", result);
        Assert.Contains("7", result);
    }

    [Fact]
    public async Task SendAsync_WithMultipleBehaviors_ShouldExecuteOuterToInner()
    {
        // Arrange
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
        });
        services.AddTransient<IPipelineBehavior<TestCommand, string>>(_ => new OrderedBehavior("A", log));
        services.AddTransient<IPipelineBehavior<TestCommand, string>>(_ => new OrderedBehavior("B", log));
        services.AddTransient<IPipelineBehavior<TestCommand, string>>(_ => new OrderedBehavior("C", log));
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        await mediator.SendAsync(new TestCommand { Name = "Order", Value = 1 });

        // Assert — Reverse wrap makes first-registered the outermost behavior
        Assert.Equal(["A:before", "B:before", "C:before", "C:after", "B:after", "A:after"], log);
    }

    private sealed class ThrowingNotificationHandler : IMediatorNotificationHandler<FailingDispatchEvent>
    {
        public Task Handle(FailingDispatchEvent notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Handler boom");
    }

    private sealed class RecordingNotificationHandler : IMediatorNotificationHandler<FailingDispatchEvent>
    {
        public static List<string> Handled { get; } = [];

        public Task Handle(FailingDispatchEvent notification, CancellationToken cancellationToken)
        {
            Handled.Add(notification.Reason);
            return Task.CompletedTask;
        }
    }

    private sealed class OrderedBehavior(string name, List<string> log) : IPipelineBehavior<TestCommand, string>
    {
        public async Task<string> Handle(
            TestCommand request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            log.Add($"{name}:before");
            string result = await next();
            log.Add($"{name}:after");
            return result;
        }
    }
}
