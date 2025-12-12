//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Observability;

/// <summary>
/// OpenTelemetry-compatible metrics for EF Core operations.
/// </summary>
/// <remarks>
/// <para>
/// This class provides comprehensive metrics for monitoring EF Core performance:
/// <list type="bullet">
/// <item><strong>Query metrics:</strong> Count, duration, slow queries</item>
/// <item><strong>Connection pool metrics:</strong> Active, idle, hits, misses</item>
/// <item><strong>Transaction metrics:</strong> Count, duration, rollbacks</item>
/// <item><strong>SaveChanges metrics:</strong> Count, entities affected</item>
/// </list>
/// </para>
/// <para>
/// Metrics follow OpenTelemetry semantic conventions and can be exported to:
/// Prometheus, Azure Monitor, Datadog, New Relic, etc.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configure OpenTelemetry metrics
/// builder.Services.AddOpenTelemetry()
///     .WithMetrics(builder =>
///     {
///         builder
///             .AddMeter(EFCoreMetrics.MeterName)
///             .AddPrometheusExporter();
///     });
/// 
/// // Register EFCoreMetrics
/// services.AddSingleton&lt;EFCoreMetrics&gt;();
/// </code>
/// </example>
public sealed class EFCoreMetrics : IDisposable
{
    /// <summary>
    /// The name of the meter for EFCore metrics.
    /// </summary>
    public const string MeterName = "Mvp24Hours.EFCore";

    private readonly Meter _meter;

    // Query metrics
    private readonly Counter<long> _queryCount;
    private readonly Counter<long> _slowQueryCount;
    private readonly Histogram<double> _queryDuration;
    private readonly Counter<long> _queryErrorCount;

    // Connection pool metrics
    private readonly ObservableGauge<int> _poolActiveConnections;
    private readonly ObservableGauge<int> _poolIdleConnections;
    private readonly Counter<long> _poolHits;
    private readonly Counter<long> _poolMisses;
    private readonly Histogram<double> _poolAcquisitionTime;

    // Transaction metrics
    private readonly Counter<long> _transactionCount;
    private readonly Counter<long> _transactionCommitCount;
    private readonly Counter<long> _transactionRollbackCount;
    private readonly Histogram<double> _transactionDuration;

    // SaveChanges metrics
    private readonly Counter<long> _saveChangesCount;
    private readonly Counter<long> _entitiesInserted;
    private readonly Counter<long> _entitiesUpdated;
    private readonly Counter<long> _entitiesDeleted;
    private readonly Histogram<double> _saveChangesDuration;

    // State for observable gauges
    private int _activeConnections;
    private int _idleConnections;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreMetrics"/>.
    /// </summary>
    /// <param name="meterFactory">Optional IMeterFactory for .NET 8+ dependency injection.</param>
    public EFCoreMetrics(IMeterFactory? meterFactory = null)
    {
        _meter = meterFactory?.Create(MeterName) ?? new Meter(MeterName, "1.0.0");

        // Query metrics
        _queryCount = _meter.CreateCounter<long>(
            "db.client.operations",
            unit: "{operation}",
            description: "Number of database operations executed");

        _slowQueryCount = _meter.CreateCounter<long>(
            "db.client.slow_queries",
            unit: "{query}",
            description: "Number of slow queries detected");

        _queryDuration = _meter.CreateHistogram<double>(
            "db.client.operation.duration",
            unit: "ms",
            description: "Duration of database operations in milliseconds");

        _queryErrorCount = _meter.CreateCounter<long>(
            "db.client.operation.errors",
            unit: "{error}",
            description: "Number of database operation errors");

        // Connection pool metrics
        _poolActiveConnections = _meter.CreateObservableGauge(
            "db.client.connections.active",
            () => _activeConnections,
            unit: "{connection}",
            description: "Number of active database connections");

        _poolIdleConnections = _meter.CreateObservableGauge(
            "db.client.connections.idle",
            () => _idleConnections,
            unit: "{connection}",
            description: "Number of idle database connections in pool");

        _poolHits = _meter.CreateCounter<long>(
            "db.client.connections.pool.hits",
            unit: "{hit}",
            description: "Number of connection pool hits");

        _poolMisses = _meter.CreateCounter<long>(
            "db.client.connections.pool.misses",
            unit: "{miss}",
            description: "Number of connection pool misses (new connections created)");

        _poolAcquisitionTime = _meter.CreateHistogram<double>(
            "db.client.connections.acquisition.duration",
            unit: "ms",
            description: "Time to acquire a connection from the pool");

        // Transaction metrics
        _transactionCount = _meter.CreateCounter<long>(
            "db.client.transactions",
            unit: "{transaction}",
            description: "Number of database transactions started");

        _transactionCommitCount = _meter.CreateCounter<long>(
            "db.client.transactions.commits",
            unit: "{commit}",
            description: "Number of committed transactions");

        _transactionRollbackCount = _meter.CreateCounter<long>(
            "db.client.transactions.rollbacks",
            unit: "{rollback}",
            description: "Number of rolled back transactions");

        _transactionDuration = _meter.CreateHistogram<double>(
            "db.client.transaction.duration",
            unit: "ms",
            description: "Duration of database transactions in milliseconds");

        // SaveChanges metrics
        _saveChangesCount = _meter.CreateCounter<long>(
            "db.client.savechanges",
            unit: "{operation}",
            description: "Number of SaveChanges operations");

        _entitiesInserted = _meter.CreateCounter<long>(
            "db.client.entities.inserted",
            unit: "{entity}",
            description: "Number of entities inserted");

        _entitiesUpdated = _meter.CreateCounter<long>(
            "db.client.entities.updated",
            unit: "{entity}",
            description: "Number of entities updated");

        _entitiesDeleted = _meter.CreateCounter<long>(
            "db.client.entities.deleted",
            unit: "{entity}",
            description: "Number of entities deleted");

        _saveChangesDuration = _meter.CreateHistogram<double>(
            "db.client.savechanges.duration",
            unit: "ms",
            description: "Duration of SaveChanges operations in milliseconds");
    }

    #region Query Metrics

    /// <summary>
    /// Records a query execution.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="operation">The operation type (SELECT, INSERT, UPDATE, DELETE).</param>
    /// <param name="dbName">Optional database name.</param>
    public void RecordQuery(double durationMs, string operation = "SELECT", string? dbName = null)
    {
        var tags = CreateTags(operation, dbName);
        _queryCount.Add(1, tags);
        _queryDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a slow query.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="thresholdMs">The slow query threshold.</param>
    /// <param name="dbName">Optional database name.</param>
    public void RecordSlowQuery(double durationMs, double thresholdMs, string? dbName = null)
    {
        var tags = new TagList
        {
            { "db.name", dbName ?? "unknown" },
            { "threshold_ms", thresholdMs.ToString("F0") }
        };
        _slowQueryCount.Add(1, tags);
    }

    /// <summary>
    /// Records a query error.
    /// </summary>
    /// <param name="errorType">The type of error.</param>
    /// <param name="dbName">Optional database name.</param>
    public void RecordQueryError(string errorType, string? dbName = null)
    {
        var tags = new TagList
        {
            { "error.type", errorType },
            { "db.name", dbName ?? "unknown" }
        };
        _queryErrorCount.Add(1, tags);
    }

    #endregion

    #region Connection Pool Metrics

    /// <summary>
    /// Updates the connection pool state.
    /// </summary>
    /// <param name="activeConnections">Number of active connections.</param>
    /// <param name="idleConnections">Number of idle connections.</param>
    public void UpdatePoolState(int activeConnections, int idleConnections)
    {
        _activeConnections = activeConnections;
        _idleConnections = idleConnections;
    }

    /// <summary>
    /// Records a connection pool hit.
    /// </summary>
    /// <param name="acquisitionTimeMs">Time to acquire the connection.</param>
    /// <param name="poolName">Optional pool name.</param>
    public void RecordPoolHit(double acquisitionTimeMs, string? poolName = null)
    {
        var tags = new TagList { { "pool.name", poolName ?? "default" } };
        _poolHits.Add(1, tags);
        _poolAcquisitionTime.Record(acquisitionTimeMs, tags);
    }

    /// <summary>
    /// Records a connection pool miss (new connection created).
    /// </summary>
    /// <param name="poolName">Optional pool name.</param>
    public void RecordPoolMiss(string? poolName = null)
    {
        var tags = new TagList { { "pool.name", poolName ?? "default" } };
        _poolMisses.Add(1, tags);
    }

    #endregion

    #region Transaction Metrics

    /// <summary>
    /// Records a transaction start.
    /// </summary>
    /// <param name="dbName">Optional database name.</param>
    public void RecordTransactionStart(string? dbName = null)
    {
        var tags = new TagList { { "db.name", dbName ?? "unknown" } };
        _transactionCount.Add(1, tags);
    }

    /// <summary>
    /// Records a transaction commit.
    /// </summary>
    /// <param name="durationMs">Duration of the transaction.</param>
    /// <param name="dbName">Optional database name.</param>
    public void RecordTransactionCommit(double durationMs, string? dbName = null)
    {
        var tags = new TagList { { "db.name", dbName ?? "unknown" } };
        _transactionCommitCount.Add(1, tags);
        _transactionDuration.Record(durationMs, tags);
    }

    /// <summary>
    /// Records a transaction rollback.
    /// </summary>
    /// <param name="durationMs">Duration of the transaction.</param>
    /// <param name="reason">Reason for rollback.</param>
    /// <param name="dbName">Optional database name.</param>
    public void RecordTransactionRollback(double durationMs, string? reason = null, string? dbName = null)
    {
        var tags = new TagList
        {
            { "db.name", dbName ?? "unknown" },
            { "rollback.reason", reason ?? "unknown" }
        };
        _transactionRollbackCount.Add(1, tags);
        _transactionDuration.Record(durationMs, tags);
    }

    #endregion

    #region SaveChanges Metrics

    /// <summary>
    /// Records a SaveChanges operation.
    /// </summary>
    /// <param name="durationMs">Duration in milliseconds.</param>
    /// <param name="inserted">Number of entities inserted.</param>
    /// <param name="updated">Number of entities updated.</param>
    /// <param name="deleted">Number of entities deleted.</param>
    /// <param name="dbName">Optional database name.</param>
    public void RecordSaveChanges(double durationMs, int inserted, int updated, int deleted, string? dbName = null)
    {
        var tags = new TagList { { "db.name", dbName ?? "unknown" } };

        _saveChangesCount.Add(1, tags);
        _saveChangesDuration.Record(durationMs, tags);

        if (inserted > 0) _entitiesInserted.Add(inserted, tags);
        if (updated > 0) _entitiesUpdated.Add(updated, tags);
        if (deleted > 0) _entitiesDeleted.Add(deleted, tags);
    }

    #endregion

    private static TagList CreateTags(string operation, string? dbName)
    {
        return new TagList
        {
            { "db.operation", operation },
            { "db.name", dbName ?? "unknown" }
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _meter.Dispose();
    }
}

