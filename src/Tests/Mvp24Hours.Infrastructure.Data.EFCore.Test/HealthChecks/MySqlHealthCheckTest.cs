using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Data.EFCore.HealthChecks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.HealthChecks;

[Trait("Category", "Unit")]
public class MySqlHealthCheckTest
{
    private static HealthCheckContext CreateContext()
    {
        return new()
        {
            Registration = new HealthCheckRegistration(
                "mysql",
                _ => throw new NotSupportedException(),
                HealthStatus.Unhealthy,
                null)
        };
    }

    [Fact]
    public void Options_Defaults_ShouldMatchExpected()
    {
        var options = new MySqlHealthCheckOptions();

        options.HealthQuery.Should().Be("SELECT 1");
        options.QueryTimeoutSeconds.Should().Be(5);
        options.DegradedThresholdMs.Should().Be(500);
        options.FailureThresholdMs.Should().Be(2000);
        options.CheckConnectionUsage.Should().BeTrue();
        options.ConnectionUsageThreshold.Should().Be(0.8);
        options.CheckSlowQueries.Should().BeFalse();
        options.CheckTableLocks.Should().BeFalse();
        options.CheckBufferPool.Should().BeFalse();
        options.CheckReplication.Should().BeFalse();
        options.ReplicationLagThresholdSeconds.Should().Be(30);
        options.Tags.Should().BeEquivalentTo(["db", "database", "mysql", "ready"]);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionOpenFails_ReturnsUnhealthy()
    {
        var check = new MySqlHealthCheck(
            "Server=invalid;Database=x;",
            new MySqlHealthCheckOptions { QueryTimeoutSeconds = 1 },
            NullLogger<MySqlHealthCheck>.Instance,
            _ => new FailingDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("failed");
        result.Data.Should().ContainKey("error");
    }

    [Fact]
    public void Constructor_WithNullConnectionString_Throws()
    {
        Func<MySqlHealthCheck> act = () => new MySqlHealthCheck(
            null!,
            new MySqlHealthCheckOptions(),
            NullLogger<MySqlHealthCheck>.Instance,
            _ => new FailingDbConnection());

        act.Should().Throw<ArgumentNullException>().WithParameterName("connectionString");
    }

    [Fact]
    public async Task CheckHealthAsync_WithSuccessfulConnection_ReturnsHealthy()
    {
        var check = new MySqlHealthCheck(
            "Server=localhost;Database=test;",
            new MySqlHealthCheckOptions { CheckConnectionUsage = false },
            NullLogger<MySqlHealthCheck>.Instance,
            _ => new SuccessfulDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("database");
        result.Data.Should().ContainKey("serverVersion");
    }

    [Fact]
    public async Task CheckHealthAsync_WithOptionalChecksEnabled_ReturnsHealthy()
    {
        var options = new MySqlHealthCheckOptions
        {
            CheckConnectionUsage = true,
            CheckSlowQueries = true,
            CheckTableLocks = true,
            CheckBufferPool = true,
            CheckReplication = true
        };
        var check = new MySqlHealthCheck(
            "Server=localhost;Database=test;",
            options,
            NullLogger<MySqlHealthCheck>.Instance,
            _ => new SuccessfulDbConnection());

        HealthCheckResult result = await check.CheckHealthAsync(CreateContext());

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("currentConnections");
        result.Data.Should().ContainKey("slowQueries");
        result.Data.Should().ContainKey("tableLockWaits");
        result.Data.Should().ContainKey("innodbBufferPoolStats");
        result.Data.Should().ContainKey("replicationStatus");
    }

    private sealed class SuccessfulDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get => "Server=localhost;Database=test;"; set => _ = value; }
        public override string Database => "test";
        public override string DataSource => "localhost";
        public override string ServerVersion => "8.0";
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
            return 1;
        }

        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<object?>(1);
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

        public override int FieldCount
        {
            get
            {
                if (commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal))
                {
                    return 4;
                }

                return _rows.Length > 0 ? _rows[0].Length : 0;
            }
        }

        public override bool HasRows => _rows.Length > 0 || commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal);
        public override bool IsClosed => false;
        public override int RecordsAffected => 0;
        public override int Depth => 0;

        public override bool Read()
        {
            _row++;
            return _row < (_rows.Length > 0 ? _rows.Length : commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal) ? 1 : 0);
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
            if (commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal))
            {
                return ordinal switch
                {
                    0 => "Slave_IO_Running",
                    1 => "Slave_SQL_Running",
                    2 => "Seconds_Behind_Master",
                    3 => "Last_Error",
                    _ => $"col{ordinal}"
                };
            }

            return ordinal switch { 0 => "Variable_name", 1 => "Value", _ => $"col{ordinal}" };
        }

        public override int GetOrdinal(string name)
        {
            return name switch
            {
                "Variable_name" => 0,
                "Value" => 1,
                "Slave_IO_Running" => 0,
                "Slave_SQL_Running" => 1,
                "Seconds_Behind_Master" => 2,
                "Last_Error" => 3,
                _ => 0
            };
        }

        public override string GetString(int ordinal)
        {
            if (commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal))
            {
                return ordinal switch
                {
                    0 => "Yes",
                    1 => "Yes",
                    2 => "0",
                    3 => string.Empty,
                    _ => string.Empty
                };
            }

            return _rows[_row][ordinal];
        }

        public override object GetValue(int ordinal)
        {
            if (commandText.Contains("SHOW SLAVE STATUS", StringComparison.Ordinal) && ordinal == 2)
            {
                return 0;
            }

            return GetString(ordinal);
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

            return [];
        }
    }

    private sealed class FailingDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get => string.Empty; set => _ = value; }
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
