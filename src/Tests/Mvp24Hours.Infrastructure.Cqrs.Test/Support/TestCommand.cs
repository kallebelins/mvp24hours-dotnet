//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Support;

/// <summary>
/// Test command that returns a string response.
/// </summary>
public class TestCommand : IMediatorCommand<string>
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

/// <summary>
/// Test command without return (Unit).
/// </summary>
public class TestVoidCommand : IMediatorCommand
{
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// Handler for TestCommand.
/// </summary>
public class TestCommandHandler : IMediatorCommandHandler<TestCommand, string>
{
    public Task<string> Handle(TestCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Executed: {request.Name} with value {request.Value}");
    }
}

/// <summary>
/// Handler for TestVoidCommand.
/// </summary>
public class TestVoidCommandHandler : IMediatorRequestHandler<TestVoidCommand>
{
    public static int ExecutionCount { get; set; }

    public Task<Unit> Handle(TestVoidCommand request, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Unit.Task;
    }
}

/// <summary>
/// Command that triggers an exception for testing.
/// </summary>
public class FailingCommand : IMediatorCommand<string>
{
    public string Message { get; set; } = "Test exception";
}

/// <summary>
/// Handler that throws an exception.
/// </summary>
public class FailingCommandHandler : IMediatorCommandHandler<FailingCommand, string>
{
    public Task<string> Handle(FailingCommand request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(request.Message);
    }
}

/// <summary>
/// Command for testing slow operations.
/// </summary>
public class SlowCommand : IMediatorCommand<string>
{
    public int DelayMs { get; set; } = 100;
}

/// <summary>
/// Handler that simulates slow execution.
/// </summary>
public class SlowCommandHandler : IMediatorCommandHandler<SlowCommand, string>
{
    public async Task<string> Handle(SlowCommand request, CancellationToken cancellationToken)
    {
        await Task.Delay(request.DelayMs, cancellationToken);
        return "Completed";
    }
}

