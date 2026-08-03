using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Observability;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Observability;

[Trait("Category", "Unit")]
public class EFCoreDiagnosticsListenerTest
{
    [Fact]
    public void DiagnosticListenerName_ShouldBeEfCore()
    {
        EFCoreDiagnosticsListener.DiagnosticListenerName.Should().Be("Microsoft.EntityFrameworkCore");
    }

    [Fact]
    public void Subscribe_ShouldNotThrow()
    {
        using var metrics = new EFCoreMetrics();
        using var listener = new EFCoreDiagnosticsListener(
            NullLogger<EFCoreDiagnosticsListener>.Instance,
            metrics);

        Action act = () => listener.Subscribe();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        using var listener = new EFCoreDiagnosticsListener();
        listener.Subscribe();

        Action act = () =>
        {
            listener.Dispose();
            listener.Dispose();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnCompleted_AndOnError_ShouldNotThrow()
    {
        using var listener = new EFCoreDiagnosticsListener(
            NullLogger<EFCoreDiagnosticsListener>.Instance);

        Action completed = () => listener.OnCompleted();
        Action error = () => listener.OnError(new InvalidOperationException("diag"));

        completed.Should().NotThrow();
        error.Should().NotThrow();
    }

    [Fact]
    public void OnNext_WithUnknownEvent_ShouldNotThrow()
    {
        using var listener = new EFCoreDiagnosticsListener();

        Action act = () => listener.OnNext(new KeyValuePair<string, object?>("Unknown.Event", null));
        act.Should().NotThrow();
    }

    [Fact]
    public void OnNext_CommandExecutingAndExecuted_ShouldNotThrow()
    {
        using var metrics = new EFCoreMetrics();
        using var listener = new EFCoreDiagnosticsListener(null, metrics);
        var commandId = Guid.NewGuid();
        var payload = new CommandEventPayload
        {
            CommandId = commandId,
            Command = new FakeDbCommand { CommandText = "SELECT * FROM Users" },
            Connection = new FakeDbConnection { Database = "AppDb" }
        };

        Action act = () =>
        {
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted", payload));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnNext_CommandError_ShouldNotThrow()
    {
        using var metrics = new EFCoreMetrics();
        using var listener = new EFCoreDiagnosticsListener(null, metrics);
        var commandId = Guid.NewGuid();
        var payload = new CommandErrorPayload
        {
            CommandId = commandId,
            Exception = new InvalidOperationException("command failed"),
            Connection = new FakeDbConnection { Database = "AppDb" }
        };

        Action act = () =>
        {
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting",
                new CommandEventPayload
                {
                    CommandId = commandId,
                    Command = new FakeDbCommand { CommandText = "UPDATE Users SET Name = 'x'" },
                    Connection = payload.Connection
                }));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandError", payload));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnNext_ConnectionEvents_ShouldLogWithoutThrowing()
    {
        using var listener = new EFCoreDiagnosticsListener(NullLogger<EFCoreDiagnosticsListener>.Instance);
        var payload = new ConnectionEventPayload { Connection = new FakeDbConnection { Database = "AppDb" } };

        Action act = () =>
        {
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpened", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosing", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionError",
                new ConnectionErrorPayload { Exception = new InvalidOperationException("connection failed") }));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnNext_TransactionEvents_ShouldNotThrow()
    {
        using var metrics = new EFCoreMetrics();
        using var listener = new EFCoreDiagnosticsListener(null, metrics);
        var payload = new TransactionEventPayload
        {
            Duration = TimeSpan.FromMilliseconds(42),
            Connection = new FakeDbConnection { Database = "AppDb" }
        };

        Action act = () =>
        {
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionStarted", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionCommitted", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionRolledBack", payload));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnNext_SaveChangesEvents_ShouldNotThrow()
    {
        using var listener = new EFCoreDiagnosticsListener(NullLogger<EFCoreDiagnosticsListener>.Instance);

        Action act = () =>
        {
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Update.SaveChangesStarting", new object()));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Update.SaveChangesCompleted", new object()));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void OnNext_CommandExecuted_WithInsertStatement_ShouldNotThrow()
    {
        using var metrics = new EFCoreMetrics();
        using var listener = new EFCoreDiagnosticsListener(null, metrics);
        var commandId = Guid.NewGuid();
        var payload = new CommandEventPayload
        {
            CommandId = commandId,
            Command = new FakeDbCommand { CommandText = "INSERT INTO Users VALUES (1)" },
            Connection = new FakeDbConnection { Database = "AppDb" }
        };

        Action act = () =>
        {
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted", payload));
            listener.OnNext(new KeyValuePair<string, object?>(
                "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted",
                new CommandEventPayload
                {
                    CommandId = commandId,
                    Command = new FakeDbCommand { CommandText = "DELETE FROM Users WHERE Id = 1" },
                    Connection = payload.Connection
                }));
        };

        act.Should().NotThrow();
    }

    private sealed class CommandEventPayload
    {
        public Guid CommandId { get; init; }
        public FakeDbCommand Command { get; init; } = new();
        public FakeDbConnection Connection { get; init; } = new();
    }

    private sealed class CommandErrorPayload
    {
        public Guid CommandId { get; init; }
        public Exception Exception { get; init; } = new InvalidOperationException();
        public FakeDbConnection Connection { get; init; } = new();
    }

    private sealed class ConnectionEventPayload
    {
        public FakeDbConnection Connection { get; init; } = new();
    }

    private sealed class ConnectionErrorPayload
    {
        public Exception Exception { get; init; } = new InvalidOperationException();
    }

    private sealed class TransactionEventPayload
    {
        public TimeSpan Duration { get; init; }
        public FakeDbConnection Connection { get; init; } = new();
    }

    private sealed class FakeDbCommand
    {
        public string CommandText { get; init; } = string.Empty;
    }

    private sealed class FakeDbConnection
    {
        public string Database { get; init; } = string.Empty;
    }
}
