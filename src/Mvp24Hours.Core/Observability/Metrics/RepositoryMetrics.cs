//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Core.Observability.Metrics;

/// <summary>
/// Provides metrics instrumentation for Repository and Data operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides counters, histograms, and gauges for monitoring
/// database operations, queries, commands, and connection health.
/// </para>
/// <para>
/// <strong>Metrics provided:</strong>
/// <list type="bullet">
/// <item><c>queries_total</c> - Counter for database queries</item>
/// <item><c>query_duration_ms</c> - Histogram for query duration</item>
/// <item><c>commands_total</c> - Counter for database commands</item>
/// <item><c>command_duration_ms</c> - Histogram for command duration</item>
/// <item><c>save_changes_total</c> - Counter for SaveChanges operations</item>
/// <item><c>save_changes_duration_ms</c> - Histogram for SaveChanges duration</item>
/// <item><c>rows_affected_total</c> - Counter for rows affected</item>
/// <item><c>connections_active</c> - Gauge for active connections</item>
/// <item><c>slow_queries_total</c> - Counter for slow queries</item>
/// <item><c>transactions_total</c> - Counter for transactions</item>
/// <item><c>transaction_rollbacks_total</c> - Counter for rollbacks</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// public class MyRepository
/// {
///     private readonly RepositoryMetrics _metrics;
///     
///     public async Task&lt;Entity&gt; GetByIdAsync(int id)
///     {
///         using var scope = _metrics.BeginQuery("GetById", "MyEntity");
///         try
///         {
///             var result = await _context.Entities.FindAsync(id);
///             scope.Complete();
///             return result;
///         }
///         catch
///         {
///             scope.Fail();
///             throw;
///         }
///     }
/// }
/// </code>
/// </remarks>
public sealed class RepositoryMetrics
{
    private readonly Counter<long> _queriesTotal;
    private readonly Counter<long> _queriesFailedTotal;
    private readonly Histogram<double> _queryDuration;
    private readonly Counter<long> _commandsTotal;
    private readonly Counter<long> _commandsFailedTotal;
    private readonly Histogram<double> _commandDuration;
    private readonly Counter<long> _saveChangesTotal;
    private readonly Histogram<double> _saveChangesDuration;
    private readonly Counter<long> _rowsAffectedTotal;
    private readonly UpDownCounter<int> _connectionsActive;
    private readonly UpDownCounter<int> _connectionsIdle;
    private readonly Counter<long> _slowQueriesTotal;
    private readonly Counter<long> _bulkOperationsTotal;
    private readonly Counter<long> _transactionsTotal;
    private readonly Counter<long> _transactionRollbacksTotal;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryMetrics"/> class.
    /// </summary>
    public RepositoryMetrics()
    {
        var meter = Mvp24HoursMeters.Data.Meter;

        _queriesTotal = meter.CreateCounter<long>(
            MetricNames.DataQueriesTotal,
            unit: "{queries}",
            description: "Total number of database queries executed");

        _queriesFailedTotal = meter.CreateCounter<long>(
            MetricNames.DataQueriesFailedTotal,
            unit: "{queries}",
            description: "Total number of failed database queries");

        _queryDuration = meter.CreateHistogram<double>(
            MetricNames.DataQueryDuration,
            unit: "ms",
            description: "Duration of database query executions in milliseconds");

        _commandsTotal = meter.CreateCounter<long>(
            MetricNames.DataCommandsTotal,
            unit: "{commands}",
            description: "Total number of database commands (insert, update, delete) executed");

        _commandsFailedTotal = meter.CreateCounter<long>(
            MetricNames.DataCommandsFailedTotal,
            unit: "{commands}",
            description: "Total number of failed database commands");

        _commandDuration = meter.CreateHistogram<double>(
            MetricNames.DataCommandDuration,
            unit: "ms",
            description: "Duration of database command executions in milliseconds");

        _saveChangesTotal = meter.CreateCounter<long>(
            MetricNames.DataSaveChangesTotal,
            unit: "{operations}",
            description: "Total number of SaveChanges operations");

        _saveChangesDuration = meter.CreateHistogram<double>(
            MetricNames.DataSaveChangesDuration,
            unit: "ms",
            description: "Duration of SaveChanges operations in milliseconds");

        _rowsAffectedTotal = meter.CreateCounter<long>(
            MetricNames.DataRowsAffectedTotal,
            unit: "{rows}",
            description: "Total number of rows affected by database operations");

        _connectionsActive = meter.CreateUpDownCounter<int>(
            MetricNames.DataConnectionsActive,
            unit: "{connections}",
            description: "Number of active database connections");

        _connectionsIdle = meter.CreateUpDownCounter<int>(
            MetricNames.DataConnectionsIdle,
            unit: "{connections}",
            description: "Number of idle database connections in pool");

        _slowQueriesTotal = meter.CreateCounter<long>(
            MetricNames.DataSlowQueriesTotal,
            unit: "{queries}",
            description: "Total number of slow queries detected");

        _bulkOperationsTotal = meter.CreateCounter<long>(
            MetricNames.DataBulkOperationsTotal,
            unit: "{operations}",
            description: "Total number of bulk operations");

        _transactionsTotal = meter.CreateCounter<long>(
            MetricNames.DataTransactionsTotal,
            unit: "{transactions}",
            description: "Total number of transactions");

        _transactionRollbacksTotal = meter.CreateCounter<long>(
            MetricNames.DataTransactionRollbacksTotal,
            unit: "{transactions}",
            description: "Total number of transaction rollbacks");
    }

    #region Query Operations

    /// <summary>
    /// Begins tracking a query operation.
    /// </summary>
    /// <param name="operationName">Name of the query operation.</param>
    /// <param name="entityType">Type of entity being queried.</param>
    /// <param name="dbSystem">Database system (optional).</param>
    /// <returns>A scope that should be disposed when query completes.</returns>
    public QueryScope BeginQuery(string operationName, string entityType, string? dbSystem = null)
    {
        return new QueryScope(this, operationName, entityType, dbSystem);
    }

    /// <summary>
    /// Records a query execution.
    /// </summary>
    /// <param name="operationName">Name of the query operation.</param>
    /// <param name="entityType">Type of entity being queried.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the query was successful.</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordQuery(
        string operationName,
        string entityType,
        double durationMs,
        bool success,
        string? dbSystem = null)
    {
        var tags = CreateQueryTags(operationName, entityType, success, dbSystem);

        _queriesTotal.Add(1, tags);

        if (!success)
        {
            _queriesFailedTotal.Add(1, tags);
        }

        _queryDuration.Record(durationMs, tags);
    }

    #endregion

    #region Command Operations

    /// <summary>
    /// Begins tracking a command operation.
    /// </summary>
    /// <param name="operationName">Name of the command operation.</param>
    /// <param name="entityType">Type of entity being affected.</param>
    /// <param name="dbSystem">Database system (optional).</param>
    /// <returns>A scope that should be disposed when command completes.</returns>
    public CommandScope BeginCommand(string operationName, string entityType, string? dbSystem = null)
    {
        return new CommandScope(this, operationName, entityType, dbSystem);
    }

    /// <summary>
    /// Records a command execution.
    /// </summary>
    /// <param name="operationName">Name of the command operation.</param>
    /// <param name="entityType">Type of entity being affected.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the command was successful.</param>
    /// <param name="rowsAffected">Number of rows affected (optional).</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordCommand(
        string operationName,
        string entityType,
        double durationMs,
        bool success,
        int rowsAffected = 0,
        string? dbSystem = null)
    {
        var tags = CreateQueryTags(operationName, entityType, success, dbSystem);

        _commandsTotal.Add(1, tags);

        if (!success)
        {
            _commandsFailedTotal.Add(1, tags);
        }

        _commandDuration.Record(durationMs, tags);

        if (rowsAffected > 0)
        {
            _rowsAffectedTotal.Add(rowsAffected, tags);
        }
    }

    #endregion

    #region SaveChanges Operations

    /// <summary>
    /// Begins tracking a SaveChanges operation.
    /// </summary>
    /// <param name="dbSystem">Database system (optional).</param>
    /// <returns>A scope that should be disposed when SaveChanges completes.</returns>
    public SaveChangesScope BeginSaveChanges(string? dbSystem = null)
    {
        return new SaveChangesScope(this, dbSystem);
    }

    /// <summary>
    /// Records a SaveChanges operation.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <param name="rowsAffected">Number of rows affected.</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordSaveChanges(
        double durationMs,
        bool success,
        int rowsAffected = 0,
        string? dbSystem = null)
    {
        var tags = new TagList
        {
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _saveChangesTotal.Add(1, tags);
        _saveChangesDuration.Record(durationMs, tags);

        if (rowsAffected > 0)
        {
            _rowsAffectedTotal.Add(rowsAffected, tags);
        }
    }

    #endregion

    #region Slow Queries

    /// <summary>
    /// Records a slow query detection.
    /// </summary>
    /// <param name="operationName">Name of the query operation.</param>
    /// <param name="entityType">Type of entity being queried.</param>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordSlowQuery(
        string operationName,
        string entityType,
        double durationMs,
        string? dbSystem = null)
    {
        var tags = new TagList
        {
            { MetricTags.Operation, operationName },
            { MetricTags.EntityType, entityType }
        };

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _slowQueriesTotal.Add(1, tags);
    }

    #endregion

    #region Bulk Operations

    /// <summary>
    /// Records a bulk operation.
    /// </summary>
    /// <param name="operationName">Name of the operation (BulkInsert, BulkUpdate, etc.).</param>
    /// <param name="entityType">Type of entity being affected.</param>
    /// <param name="rowsAffected">Number of rows affected.</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordBulkOperation(
        string operationName,
        string entityType,
        int rowsAffected,
        string? dbSystem = null)
    {
        var tags = new TagList
        {
            { MetricTags.Operation, operationName },
            { MetricTags.EntityType, entityType }
        };

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _bulkOperationsTotal.Add(1, tags);
        _rowsAffectedTotal.Add(rowsAffected, tags);
    }

    #endregion

    #region Transactions

    /// <summary>
    /// Records a transaction start.
    /// </summary>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordTransactionStart(string? dbSystem = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _transactionsTotal.Add(1, tags);
    }

    /// <summary>
    /// Records a transaction rollback.
    /// </summary>
    /// <param name="dbSystem">Database system (optional).</param>
    public void RecordTransactionRollback(string? dbSystem = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _transactionRollbacksTotal.Add(1, tags);
    }

    #endregion

    #region Connection Pool

    /// <summary>
    /// Updates the active connection count.
    /// </summary>
    /// <param name="delta">Change in connection count (+1 or -1).</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void UpdateActiveConnections(int delta, string? dbSystem = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _connectionsActive.Add(delta, tags);
    }

    /// <summary>
    /// Updates the idle connection count.
    /// </summary>
    /// <param name="delta">Change in connection count (+1 or -1).</param>
    /// <param name="dbSystem">Database system (optional).</param>
    public void UpdateIdleConnections(int delta, string? dbSystem = null)
    {
        var tags = new TagList();

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        _connectionsIdle.Add(delta, tags);
    }

    #endregion

    #region Helper Methods

    private static TagList CreateQueryTags(
        string operationName,
        string entityType,
        bool success,
        string? dbSystem)
    {
        var tags = new TagList
        {
            { MetricTags.Operation, operationName },
            { MetricTags.EntityType, entityType },
            { MetricTags.Status, success ? MetricTags.StatusSuccess : MetricTags.StatusFailure }
        };

        if (!string.IsNullOrEmpty(dbSystem))
        {
            tags.Add(MetricTags.DbSystem, dbSystem);
        }

        return tags;
    }

    #endregion

    #region Scope Structs

    /// <summary>
    /// Represents a scope for tracking query execution duration.
    /// </summary>
    public readonly struct QueryScope : IDisposable
    {
        private readonly RepositoryMetrics _metrics;
        private readonly string _operationName;
        private readonly string _entityType;
        private readonly string? _dbSystem;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the query succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        internal QueryScope(RepositoryMetrics metrics, string operationName, string entityType, string? dbSystem)
        {
            _metrics = metrics;
            _operationName = operationName;
            _entityType = entityType;
            _dbSystem = dbSystem;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
        }

        /// <summary>
        /// Marks the query as completed successfully.
        /// </summary>
        public void Complete() => Succeeded = true;

        /// <summary>
        /// Marks the query as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordQuery(_operationName, _entityType, elapsed.TotalMilliseconds, Succeeded, _dbSystem);
        }
    }

    /// <summary>
    /// Represents a scope for tracking command execution duration.
    /// </summary>
    public readonly struct CommandScope : IDisposable
    {
        private readonly RepositoryMetrics _metrics;
        private readonly string _operationName;
        private readonly string _entityType;
        private readonly string? _dbSystem;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the command succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Gets or sets the number of rows affected.
        /// </summary>
        public int RowsAffected { get; private set; }

        internal CommandScope(RepositoryMetrics metrics, string operationName, string entityType, string? dbSystem)
        {
            _metrics = metrics;
            _operationName = operationName;
            _entityType = entityType;
            _dbSystem = dbSystem;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
            RowsAffected = 0;
        }

        /// <summary>
        /// Marks the command as completed successfully.
        /// </summary>
        /// <param name="rowsAffected">Number of rows affected.</param>
        public void Complete(int rowsAffected = 0)
        {
            Succeeded = true;
            RowsAffected = rowsAffected;
        }

        /// <summary>
        /// Marks the command as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordCommand(_operationName, _entityType, elapsed.TotalMilliseconds, Succeeded, RowsAffected, _dbSystem);
        }
    }

    /// <summary>
    /// Represents a scope for tracking SaveChanges execution duration.
    /// </summary>
    public readonly struct SaveChangesScope : IDisposable
    {
        private readonly RepositoryMetrics _metrics;
        private readonly string? _dbSystem;
        private readonly long _startTimestamp;

        /// <summary>
        /// Gets or sets whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Gets or sets the number of rows affected.
        /// </summary>
        public int RowsAffected { get; private set; }

        internal SaveChangesScope(RepositoryMetrics metrics, string? dbSystem)
        {
            _metrics = metrics;
            _dbSystem = dbSystem;
            _startTimestamp = Stopwatch.GetTimestamp();
            Succeeded = false;
            RowsAffected = 0;
        }

        /// <summary>
        /// Marks the operation as completed successfully.
        /// </summary>
        /// <param name="rowsAffected">Number of rows affected.</param>
        public void Complete(int rowsAffected = 0)
        {
            Succeeded = true;
            RowsAffected = rowsAffected;
        }

        /// <summary>
        /// Marks the operation as failed.
        /// </summary>
        public void Fail() => Succeeded = false;

        /// <inheritdoc />
        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp);
            _metrics.RecordSaveChanges(elapsed.TotalMilliseconds, Succeeded, RowsAffected, _dbSystem);
        }
    }

    #endregion
}

