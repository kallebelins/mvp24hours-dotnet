using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class PostgreSqlHealthCheckTest
{
    private static HealthCheckContext CreateContext()
    {
        return new()
        {
            Registration = new HealthCheckRegistration(
                "postgresql",
                _ => throw new NotSupportedException(),
                HealthStatus.Unhealthy,
                null)
        };
    }

    [Fact]
    public void Options_Defaults_ShouldMatchExpected()
    {
        var options = new PostgreSqlHealthCheckOptions();

        options.HealthQuery.Should().Be("SELECT 1");
        options.QueryTimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.CheckConnectionUsage.Should().BeTrue();
        options.ConnectionUsageThreshold.Should().Be(0.8);
        options.CheckReplicationLag.Should().BeFalse();
        options.CheckDatabaseSize.Should().BeFalse();
        options.CheckLocks.Should().BeFalse();
        options.BlockedLocksThreshold.Should().Be(10);
        options.Tags.Should().BeEquivalentTo(["db", "database", "postgresql", "ready"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionOpenFails_ReturnsUnhealthy()
    {
        var check = new PostgreSqlHealthCheck(
            "Host=invalid;Database=x;",
            new PostgreSqlHealthCheckOptions { QueryTimeoutSeconds = 1 },
            NullLogger<PostgreSqlHealthCheck>.Instance,
            _ => new FailingDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
        result.Data.Should().ContainKey("error");
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_Throws()
    {
        Func<PostgreSqlHealthCheck> act = () => new PostgreSqlHealthCheck(
            "Host=localhost;",
            new PostgreSqlHealthCheckOptions(),
            NullLogger<PostgreSqlHealthCheck>.Instance,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionFactory");
    }

    [Fact]
    public async Task CheckHealthAsync_WithSuccessfulConnection_ReturnsHealthy()
    {
        var check = new PostgreSqlHealthCheck(
            "Host=localhost;Database=test;",
            new PostgreSqlHealthCheckOptions { CheckConnectionUsage = false },
            NullLogger<PostgreSqlHealthCheck>.Instance,
            _ => new SuccessfulDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("database");
        result.Data.Should().ContainKey("serverVersion");
    }

    [Fact]
    public async Task CheckHealthAsync_WithOptionalChecksEnabled_ReturnsHealthy()
    {
        var options = new PostgreSqlHealthCheckOptions
        {
            CheckConnectionUsage = true,
            CheckReplicationLag = true,
            CheckDatabaseSize = true,
            CheckLocks = true
        };
        var check = new PostgreSqlHealthCheck(
            "Host=localhost;Database=test;",
            options,
            NullLogger<PostgreSqlHealthCheck>.Instance,
            _ => new SuccessfulDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("currentConnections");
        result.Data.Should().ContainKey("databaseSizeBytes");
        result.Data.Should().ContainKey("blockedLocks");
    }

    private sealed class SuccessfulDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get => "Host=localhost;Database=test;"; set => _ = value; }
        public override string Database => "test";
        public override string DataSource => "localhost";
        public override string ServerVersion => "14.0";
        public override ConnectionState State => ConnectionState.Open;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            return new SuccessfulDbCommand();
        }
    }

    private sealed class SuccessfulDbCommand : DbCommand
    {
        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel() { }
        public override int ExecuteNonQuery()
        {
            return 1;
        }

        public override void Prepare() { }
        public override object? ExecuteScalar()
        {
            if (CommandText.Contains("pg_database_size", StringComparison.Ordinal))
            {
                return 1024L;
            }

            if (CommandText.Contains("pg_locks", StringComparison.Ordinal))
            {
                return 0;
            }

            if (CommandText.Contains("pg_is_in_recovery", StringComparison.Ordinal))
            {
                return DBNull.Value;
            }

            return 1;
        }

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(ExecuteScalar());
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotSupportedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new SuccessfulDbDataReader(CommandText);
        }
    }

    private sealed class SuccessfulDbDataReader(string commandText) : DbDataReader
    {
        private int _row = -1;
        private readonly string[][] _rows = BuildRows(commandText);

        public override int FieldCount => _rows.Length > 0 ? _rows[0].Length : 0;
        public override bool HasRows => _rows.Length > 0;
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;

        public override bool Read()
        {
            _row++;
            return _row < _rows.Length;
        }

        public override Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Read());
        }

        public override bool GetBoolean(int ordinal)
        {
            return bool.Parse(GetString(ordinal));
        }

        public override byte GetByte(int ordinal)
        {
            return byte.Parse(GetString(ordinal));
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            throw new NotSupportedException();
        }

        public override char GetChar(int ordinal)
        {
            return GetString(ordinal)[0];
        }

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            throw new NotSupportedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            return "text";
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return DateTime.Parse(GetString(ordinal));
        }

        public override decimal GetDecimal(int ordinal)
        {
            return decimal.Parse(GetString(ordinal));
        }

        public override double GetDouble(int ordinal)
        {
            return double.Parse(GetString(ordinal));
        }

        public override float GetFloat(int ordinal)
        {
            return float.Parse(GetString(ordinal));
        }

        public override Guid GetGuid(int ordinal)
        {
            return Guid.Parse(GetString(ordinal));
        }

        public override short GetInt16(int ordinal)
        {
            return short.Parse(GetString(ordinal));
        }

        public override int GetInt32(int ordinal)
        {
            return int.Parse(GetString(ordinal));
        }

        public override long GetInt64(int ordinal)
        {
            return long.Parse(GetString(ordinal));
        }

        public override string GetName(int ordinal)
        {
            return ordinal switch { 0 => "col0", 1 => "col1", _ => $"col{ordinal}" };
        }

        public override int GetOrdinal(string name)
        {
            return name switch { "col0" or "Variable_name" => 0, "col1" or "Value" => 1, _ => 0 };
        }

        public override string GetString(int ordinal)
        {
            return _rows[_row][ordinal];
        }

        public override object GetValue(int ordinal)
        {
            return _rows[_row][ordinal];
        }

        public override int GetValues(object[] values)
        {
            for (int i = 0; i < FieldCount; i++)
            {
                values[i] = GetValue(i);
            }

            return FieldCount;
        }

        public override bool IsDBNull(int ordinal)
        {
            return GetValue(ordinal) == DBNull.Value;
        }

        public override int VisibleFieldCount => FieldCount;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override bool NextResult()
        {
            return false;
        }

        public override Type GetFieldType(int ordinal)
        {
            return typeof(string);
        }

        public override System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotSupportedException();
        }

        private static string[][] BuildRows(string commandText)
        {
            if (commandText.Contains("pg_stat_activity", StringComparison.Ordinal))
            {
                return [["10", "100"]];
            }

            if (commandText.Contains("SHOW GLOBAL STATUS", StringComparison.Ordinal))
            {
                return
                [
                    ["Threads_connected", "10"],
                    ["Slow_queries", "0"],
                    ["Table_locks_waited", "0"],
                    ["Questions", "1000"],
                    ["Uptime", "3600"]
                ];
            }

            if (commandText.Contains("SHOW GLOBAL VARIABLES", StringComparison.Ordinal))
            {
                return [["max_connections", "100"]];
            }

            if (commandText.Contains("Innodb_buffer_pool", StringComparison.Ordinal))
            {
                return
                [
                    ["Innodb_buffer_pool_read_requests", "1000"],
                    ["Innodb_buffer_pool_reads", "10"]
                ];
            }

            if (commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal))
            {
                return [["Yes", "Yes", "0", ""]];
            }

            return [];
        }
    }

    private sealed class FailingDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString
        {
            get => string.Empty;
            set => _ = value;
        }
        public override string Database => "test";
        public override string DataSource => "test";
        public override string ServerVersion => "0";
        public override ConnectionState State => ConnectionState.Closed;

        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open()
        {
            throw new InvalidOperationException("Simulated connection failure");
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return Task.FromException(new InvalidOperationException("Simulated connection failure"));
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            throw new NotSupportedException();
        }
    }
}
