//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Handlers;

/// <summary>
/// Phase 24.1 — LoggingBehavior success/failure logging and null-logger guard.
/// </summary>
[Trait("Category", "Unit")]
public class LoggingBehaviorTest
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingBehavior<TestCommand, string>(null!));
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldLogStartingAndCompleted()
    {
        // Arrange
        var logger = new CollectingLogger<LoggingBehavior<TestCommand, string>>();
        var behavior = new LoggingBehavior<TestCommand, string>(logger);

        // Act
        string result = await behavior.Handle(
            new TestCommand { Name = "Log", Value = 1 },
            () => Task.FromResult("ok"),
            CancellationToken.None);

        // Assert
        Assert.Equal("ok", result);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Starting"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Completed"));
        Assert.Contains(logger.Entries, e => e.Message.Contains(nameof(TestCommand)));
    }

    [Fact]
    public async Task Handle_OnSuccess_ShouldIncludeRequestIdInLogs()
    {
        // Arrange
        var logger = new CollectingLogger<LoggingBehavior<TestCommand, string>>();
        var behavior = new LoggingBehavior<TestCommand, string>(logger);

        // Act
        await behavior.Handle(
            new TestCommand { Name = "Id", Value = 2 },
            () => Task.FromResult("ok"),
            CancellationToken.None);

        // Assert — request id is 8-char hex from Guid
        string start = Assert.Single(logger.Entries, e => e.Message.Contains("Starting")).Message;
        string completed = Assert.Single(logger.Entries, e => e.Message.Contains("Completed")).Message;
        Assert.Matches(@"ID: [0-9a-f]{8}", start);
        Assert.Matches(@"ID: [0-9a-f]{8}", completed);
    }

    [Fact]
    public async Task Handle_OnFailure_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var logger = new CollectingLogger<LoggingBehavior<TestCommand, string>>();
        var behavior = new LoggingBehavior<TestCommand, string>(logger);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TestCommand { Name = "Fail", Value = 3 },
                () => throw new InvalidOperationException("handler failed"),
                CancellationToken.None));

        // Assert
        Assert.Equal("handler failed", ex.Message);
        (LogLevel Level, string Message, Exception? Exception) error =
            Assert.Single(logger.Entries, e => e.Level == LogLevel.Error);
        Assert.Contains("Failed", error.Message);
        Assert.Contains(nameof(TestCommand), error.Message);
        Assert.NotNull(error.Exception);
    }

    [Fact]
    public async Task Handle_ViaMediator_ShouldLogWhenBehaviorRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly);
            options.RegisterLoggingBehavior = true;
        });
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act
        string result = await mediator.SendAsync(new TestCommand { Name = "Pipeline", Value = 9 });

        // Assert
        Assert.Contains("Pipeline", result);
    }

    [Fact]
    public async Task Handle_ViaMediator_OnFailure_ShouldStillPropagate()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
        {
            options.RegisterHandlersFromAssembly(typeof(FailingCommand).Assembly);
            options.RegisterLoggingBehavior = true;
        });
        IMediator mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new FailingCommand { Message = "logged failure" }));
    }
}
