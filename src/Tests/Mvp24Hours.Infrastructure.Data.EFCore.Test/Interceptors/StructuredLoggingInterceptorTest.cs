using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Testing.Logging;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class StructuredLoggingInterceptorTest
{
    [Fact]
    public void SaveChanges_WithNullLogger_DoesNotThrow()
    {
        var interceptor = new StructuredLoggingInterceptor();

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Structured" });
        Func<int> act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void SaveChanges_WithLogger_DoesNotThrow()
    {
        var interceptor = new StructuredLoggingInterceptor(
            NullLogger.Instance,
            logParameters: false,
            outputAsJson: true);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        context.Entities.Add(new TestEntity { Name = "Structured" });
        Func<int> act = () => context.SaveChanges();

        act.Should().NotThrow();
    }

    [Fact]
    public void NonQueryExecuted_WithStructuredLogger_ShouldEmitInsertLog()
    {
        var logger = new FakeLogger<StructuredLoggingInterceptor> { MinimumLevel = LogLevel.Debug };
        var interceptor = new StructuredLoggingInterceptor(logger, logParameters: true, commandLogLevel: LogLevel.Debug);
        using TestDbCommand command = CreateCommand("INSERT INTO Entities (Name) VALUES (@p0)", ("@p0", "secret"));

        interceptor.NonQueryExecuted(command, CreateEventData(), 1);

        logger.Logs.Should().Contain(entry => entry.Message.Contains("INSERT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NonQueryExecuted_WithJsonOutput_ShouldEmitJsonPayload()
    {
        var logger = new FakeLogger<StructuredLoggingInterceptor> { MinimumLevel = LogLevel.Debug };
        var interceptor = new StructuredLoggingInterceptor(logger, logParameters: false, outputAsJson: true, commandLogLevel: LogLevel.Debug);
        using TestDbCommand command = CreateCommand("INSERT INTO Entities (Name) VALUES ('x')");

        interceptor.NonQueryExecuted(command, CreateEventData(), 1);

        logger.Logs.Should().Contain(entry => entry.Message.Contains("EFCore.Command"));
    }

    [Fact]
    public void ReaderExecuted_WithSensitiveParameter_ShouldMaskValue()
    {
        var logger = new FakeLogger<StructuredLoggingInterceptor> { MinimumLevel = LogLevel.Debug };
        var interceptor = new StructuredLoggingInterceptor(
            logger,
            logParameters: true,
            sensitiveParameters: ["password"],
            commandLogLevel: LogLevel.Debug);
        using TestDbCommand command = CreateCommand("SELECT * FROM Users WHERE password = @password", ("@password", "secret"));

        interceptor.ReaderExecuted(command, CreateEventData(), null!);

        logger.Logs.Should().Contain(entry => entry.Message.Contains("***MASKED***"));
    }

    [Fact]
    public void ScalarExecutedAsync_ShouldLogWithoutThrowing()
    {
        var logger = new FakeLogger<StructuredLoggingInterceptor> { MinimumLevel = LogLevel.Debug };
        var interceptor = new StructuredLoggingInterceptor(logger, commandLogLevel: LogLevel.Debug);
        using TestDbCommand command = CreateCommand("SELECT COUNT(*) FROM Entities");

        Func<Task> act = async () =>
            await interceptor.ScalarExecutedAsync(command, CreateEventData(), 1, CancellationToken.None);

        act.Should().NotThrowAsync();
        logger.Logs.Should().Contain(entry => entry.Message.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CommandFailed_ShouldLogError()
    {
        var logger = new FakeLogger<StructuredLoggingInterceptor> { MinimumLevel = LogLevel.Debug };
        var interceptor = new StructuredLoggingInterceptor(logger, errorLogLevel: LogLevel.Error);
        using TestDbCommand command = CreateCommand("UPDATE Entities SET Name = 'x'");

        interceptor.CommandFailed(command, CreateErrorEventData(command, new InvalidOperationException("boom")));

        logger.Logs.Should().Contain(entry => entry.LogLevel == LogLevel.Error);
    }

    private static CommandExecutedEventData CreateEventData()
    {
        return new CommandExecutedEventData(
            null!,
            (_, _) => string.Empty,
            null!,
            null!,
            string.Empty,
            null,
            DbCommandMethod.ExecuteNonQuery,
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            false,
            false,
            DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(5),
            CommandSource.Unknown);
    }

    private static CommandErrorEventData CreateErrorEventData(DbCommand command, Exception exception)
    {
        return new CommandErrorEventData(
            null!,
            (_, _) => string.Empty,
            null!,
            command,
            string.Empty,
            null,
            DbCommandMethod.ExecuteNonQuery,
            Guid.NewGuid(),
            Guid.NewGuid(),
            exception,
            false,
            false,
            DateTimeOffset.UtcNow,
            TimeSpan.FromMilliseconds(12),
            CommandSource.Unknown);
    }

    private static TestDbCommand CreateCommand(string sql, params (string Name, object? Value)[] parameters)
    {
        var command = new TestDbCommand { CommandText = sql };
        foreach ((string name, object? value) in parameters)
        {
            command.Parameters.Add(new TestDbParameter(name, value));
        }

        return command;
    }

#pragma warning disable CS8765, CS8764
    private sealed class TestDbCommand : DbCommand
    {
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; } = CommandType.Text;
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new TestDbParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery()
        {
            return 0;
        }

        public override object? ExecuteScalar()
        {
            return null;
        }

        public override void Prepare() { }
        protected override DbParameter CreateDbParameter()
        {
            return new TestDbParameter("@p", null);
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestDbParameterCollection : DbParameterCollection
    {
        private readonly List<TestDbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot { get; } = new();
        public override int Add(object value)
        {
            _parameters.Add((TestDbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (object value in values)
            {
                Add(value);
            }
        }

        public override void Clear()
        {
            _parameters.Clear();
        }

        public override bool Contains(object value)
        {
            return _parameters.Contains((TestDbParameter)value);
        }

        public override bool Contains(string value)
        {
            return _parameters.Any(p => p.ParameterName == value);
        }

        public override void CopyTo(Array array, int index)
        {
            _parameters.ToArray().CopyTo(array, index);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            return _parameters.GetEnumerator();
        }

        public override int IndexOf(object value)
        {
            return _parameters.IndexOf((TestDbParameter)value);
        }

        public override int IndexOf(string parameterName)
        {
            return _parameters.FindIndex(p => p.ParameterName == parameterName);
        }

        public override void Insert(int index, object value)
        {
            _parameters.Insert(index, (TestDbParameter)value);
        }

        public override void Remove(object value)
        {
            _parameters.Remove((TestDbParameter)value);
        }

        public override void RemoveAt(int index)
        {
            _parameters.RemoveAt(index);
        }

        public override void RemoveAt(string parameterName)
        {
            int index = IndexOf(parameterName);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        protected override DbParameter GetParameter(int index)
        {
            return _parameters[index];
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            return _parameters.First(p => p.ParameterName == parameterName);
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            _parameters[index] = (TestDbParameter)value;
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            int index = IndexOf(parameterName);
            if (index >= 0)
            {
                _parameters[index] = (TestDbParameter)value;
            }
        }
    }

    private sealed class TestDbParameter(string name, object? value) : DbParameter
    {
        public override DbType DbType { get; set; } = DbType.String;
        public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
        public override bool IsNullable { get; set; } = true;
        public override string ParameterName { get; set; } = name;
        public override int Size { get; set; }
        public override string SourceColumn { get; set; } = string.Empty;
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; } = value;
        public override void ResetDbType() { }
    }
#pragma warning restore CS8765, CS8764
}
