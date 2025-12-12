//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Observability;

/// <summary>
/// Diagnostic listener that captures EF Core events and creates OpenTelemetry activities.
/// </summary>
/// <remarks>
/// <para>
/// This listener subscribes to EF Core diagnostic events and converts them to OpenTelemetry spans.
/// It provides comprehensive observability for:
/// <list type="bullet">
/// <item>Database connections (opening, closing, errors)</item>
/// <item>Command execution (before, after, errors)</item>
/// <item>Transactions (starting, committing, rolling back)</item>
/// <item>SaveChanges operations</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage:</strong> Register using the extension method:
/// <code>
/// services.AddEFCoreDiagnosticsListener();
/// </code>
/// </para>
/// </remarks>
public sealed class EFCoreDiagnosticsListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly ILogger<EFCoreDiagnosticsListener>? _logger;
    private readonly EFCoreMetrics? _metrics;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly Dictionary<Guid, Activity> _commandActivities = new();
    private readonly Dictionary<Guid, Stopwatch> _commandTimings = new();
    private readonly object _lock = new();

    /// <summary>
    /// The diagnostic listener name for EF Core.
    /// </summary>
    public const string DiagnosticListenerName = "Microsoft.EntityFrameworkCore";

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreDiagnosticsListener"/>.
    /// </summary>
    public EFCoreDiagnosticsListener(
        ILogger<EFCoreDiagnosticsListener>? logger = null,
        EFCoreMetrics? metrics = null)
    {
        _logger = logger;
        _metrics = metrics;
    }

    /// <summary>
    /// Subscribes to the diagnostic listener.
    /// </summary>
    public void Subscribe()
    {
        var subscription = DiagnosticListener.AllListeners.Subscribe(this);
        lock (_lock)
        {
            _subscriptions.Add(subscription);
        }
    }

    /// <inheritdoc />
    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == DiagnosticListenerName)
        {
            var subscription = listener.Subscribe(this);
            lock (_lock)
            {
                _subscriptions.Add(subscription);
            }
        }
    }

    /// <inheritdoc />
    public void OnNext(KeyValuePair<string, object?> value)
    {
        switch (value.Key)
        {
            // Command Events
            case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting":
                OnCommandExecuting(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted":
                OnCommandExecuted(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Command.CommandError":
                OnCommandError(value.Value);
                break;

            // Connection Events
            case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpening":
                OnConnectionOpening(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionOpened":
                OnConnectionOpened(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionClosing":
                OnConnectionClosing(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Connection.ConnectionError":
                OnConnectionError(value.Value);
                break;

            // Transaction Events
            case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionStarted":
                OnTransactionStarted(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionCommitted":
                OnTransactionCommitted(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Database.Transaction.TransactionRolledBack":
                OnTransactionRolledBack(value.Value);
                break;

            // SaveChanges Events
            case "Microsoft.EntityFrameworkCore.Update.SaveChangesStarting":
                OnSaveChangesStarting(value.Value);
                break;

            case "Microsoft.EntityFrameworkCore.Update.SaveChangesCompleted":
                OnSaveChangesCompleted(value.Value);
                break;
        }
    }

    #region Command Event Handlers

    private void OnCommandExecuting(object? payload)
    {
        if (payload == null) return;

        var commandId = GetCommandId(payload);
        var commandText = GetCommandText(payload);
        var dbName = GetDatabaseName(payload);

        if (commandId == Guid.Empty) return;

        // Start activity
        var activity = EFCoreActivitySource.Source.StartActivity(
            EFCoreActivitySource.ActivityNames.Query,
            ActivityKind.Client);

        if (activity != null)
        {
            activity.SetTag(EFCoreActivitySource.TagNames.DbStatement, commandText);
            if (!string.IsNullOrEmpty(dbName))
                activity.SetTag(EFCoreActivitySource.TagNames.DbName, dbName);

            lock (_lock)
            {
                _commandActivities[commandId] = activity;
            }
        }

        // Start timing
        lock (_lock)
        {
            _commandTimings[commandId] = Stopwatch.StartNew();
        }
    }

    private void OnCommandExecuted(object? payload)
    {
        if (payload == null) return;

        var commandId = GetCommandId(payload);
        if (commandId == Guid.Empty) return;

        Activity? activity = null;
        Stopwatch? stopwatch = null;

        lock (_lock)
        {
            _commandActivities.TryGetValue(commandId, out activity);
            _commandTimings.TryGetValue(commandId, out stopwatch);
            _commandActivities.Remove(commandId);
            _commandTimings.Remove(commandId);
        }

        stopwatch?.Stop();
        var durationMs = stopwatch?.Elapsed.TotalMilliseconds ?? 0;

        if (activity != null)
        {
            EFCoreActivitySource.SetDuration(activity, durationMs);
            EFCoreActivitySource.SetSuccess(activity);
            activity.Dispose();
        }

        // Record metrics
        var operation = GetOperation(payload);
        var dbName = GetDatabaseName(payload);
        _metrics?.RecordQuery(durationMs, operation, dbName);
    }

    private void OnCommandError(object? payload)
    {
        if (payload == null) return;

        var commandId = GetCommandId(payload);
        if (commandId == Guid.Empty) return;

        Activity? activity = null;
        Stopwatch? stopwatch = null;

        lock (_lock)
        {
            _commandActivities.TryGetValue(commandId, out activity);
            _commandTimings.TryGetValue(commandId, out stopwatch);
            _commandActivities.Remove(commandId);
            _commandTimings.Remove(commandId);
        }

        stopwatch?.Stop();

        var exception = GetException(payload);
        if (activity != null)
        {
            if (exception != null)
                EFCoreActivitySource.SetError(activity, exception);
            activity.Dispose();
        }

        // Record error metrics
        var dbName = GetDatabaseName(payload);
        _metrics?.RecordQueryError(exception?.GetType().Name ?? "Unknown", dbName);
    }

    #endregion

    #region Connection Event Handlers

    private void OnConnectionOpening(object? payload)
    {
        _logger?.LogDebug("Database connection opening");
    }

    private void OnConnectionOpened(object? payload)
    {
        _logger?.LogDebug("Database connection opened");
    }

    private void OnConnectionClosing(object? payload)
    {
        _logger?.LogDebug("Database connection closing");
    }

    private void OnConnectionError(object? payload)
    {
        var exception = GetException(payload);
        _logger?.LogError(exception, "Database connection error");
    }

    #endregion

    #region Transaction Event Handlers

    private void OnTransactionStarted(object? payload)
    {
        var dbName = GetDatabaseName(payload);
        _metrics?.RecordTransactionStart(dbName);
        _logger?.LogDebug("Transaction started on database {Database}", dbName);
    }

    private void OnTransactionCommitted(object? payload)
    {
        var dbName = GetDatabaseName(payload);
        var duration = GetDuration(payload);
        _metrics?.RecordTransactionCommit(duration.TotalMilliseconds, dbName);
        _logger?.LogDebug("Transaction committed on database {Database} after {DurationMs:F2}ms", dbName, duration.TotalMilliseconds);
    }

    private void OnTransactionRolledBack(object? payload)
    {
        var dbName = GetDatabaseName(payload);
        var duration = GetDuration(payload);
        _metrics?.RecordTransactionRollback(duration.TotalMilliseconds, null, dbName);
        _logger?.LogWarning("Transaction rolled back on database {Database} after {DurationMs:F2}ms", dbName, duration.TotalMilliseconds);
    }

    #endregion

    #region SaveChanges Event Handlers

    private void OnSaveChangesStarting(object? payload)
    {
        _logger?.LogDebug("SaveChanges starting");
    }

    private void OnSaveChangesCompleted(object? payload)
    {
        _logger?.LogDebug("SaveChanges completed");
    }

    #endregion

    #region Payload Extractors

    private static Guid GetCommandId(object? payload)
    {
        if (payload == null) return Guid.Empty;

        var type = payload.GetType();
        var prop = type.GetProperty("CommandId");
        if (prop?.GetValue(payload) is Guid id)
            return id;

        return Guid.Empty;
    }

    private static string GetCommandText(object? payload)
    {
        if (payload == null) return string.Empty;

        var type = payload.GetType();
        var commandProp = type.GetProperty("Command");
        var command = commandProp?.GetValue(payload);
        if (command == null) return string.Empty;

        var textProp = command.GetType().GetProperty("CommandText");
        return textProp?.GetValue(command) as string ?? string.Empty;
    }

    private static string? GetDatabaseName(object? payload)
    {
        if (payload == null) return null;

        var type = payload.GetType();

        // Try Connection property
        var connectionProp = type.GetProperty("Connection");
        var connection = connectionProp?.GetValue(payload);
        if (connection != null)
        {
            var dbProp = connection.GetType().GetProperty("Database");
            return dbProp?.GetValue(connection) as string;
        }

        // Try DbContext property
        var contextProp = type.GetProperty("Context");
        var context = contextProp?.GetValue(payload);
        if (context != null)
        {
            var databaseProp = context.GetType().GetProperty("Database");
            var database = databaseProp?.GetValue(context);
            if (database != null)
            {
                var currentDbProp = database.GetType().GetProperty("ProviderName");
                return currentDbProp?.GetValue(database) as string;
            }
        }

        return null;
    }

    private static Exception? GetException(object? payload)
    {
        if (payload == null) return null;

        var type = payload.GetType();
        var prop = type.GetProperty("Exception");
        return prop?.GetValue(payload) as Exception;
    }

    private static TimeSpan GetDuration(object? payload)
    {
        if (payload == null) return TimeSpan.Zero;

        var type = payload.GetType();
        var prop = type.GetProperty("Duration");
        if (prop?.GetValue(payload) is TimeSpan duration)
            return duration;

        return TimeSpan.Zero;
    }

    private static string GetOperation(object? payload)
    {
        var commandText = GetCommandText(payload);
        if (string.IsNullOrEmpty(commandText)) return "UNKNOWN";

        var normalized = commandText.TrimStart().ToUpperInvariant();
        if (normalized.StartsWith("SELECT")) return "SELECT";
        if (normalized.StartsWith("INSERT")) return "INSERT";
        if (normalized.StartsWith("UPDATE")) return "UPDATE";
        if (normalized.StartsWith("DELETE")) return "DELETE";

        return "OTHER";
    }

    #endregion

    /// <inheritdoc />
    public void OnError(Exception error)
    {
        _logger?.LogError(error, "Error in EFCore diagnostics listener");
    }

    /// <inheritdoc />
    public void OnCompleted()
    {
        // No action needed
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            foreach (var activity in _commandActivities.Values)
            {
                activity.Dispose();
            }
            _commandActivities.Clear();
            _commandTimings.Clear();
        }
    }
}

