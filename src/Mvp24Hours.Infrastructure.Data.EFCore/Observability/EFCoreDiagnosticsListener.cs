//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

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
/// <remarks>
/// Initializes a new instance of <see cref="EFCoreDiagnosticsListener"/>.
/// </remarks>
public sealed class EFCoreDiagnosticsListener(
    ILogger<EFCoreDiagnosticsListener>? logger = null,
    EFCoreMetrics? metrics = null) : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly ILogger<EFCoreDiagnosticsListener>? _logger = logger;
    private readonly EFCoreMetrics? _metrics = metrics;
    private readonly List<IDisposable> _subscriptions = [];
    private readonly Dictionary<Guid, Activity> _commandActivities = [];
    private readonly Dictionary<Guid, Stopwatch> _commandTimings = [];
    private readonly object _lock = new();

    /// <summary>
    /// The diagnostic listener name for EF Core.
    /// </summary>
    public const string DiagnosticListenerName = "Microsoft.EntityFrameworkCore";

    /// <summary>
    /// Subscribes to the diagnostic listener.
    /// </summary>
    public void Subscribe()
    {
        IDisposable subscription = DiagnosticListener.AllListeners.Subscribe(this);
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
            IDisposable subscription = listener.Subscribe(this);
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
        if (payload == null)
        {
            return;
        }

        Guid commandId = GetCommandId(payload);
        string commandText = GetCommandText(payload);
        string? dbName = GetDatabaseName(payload);

        if (commandId == Guid.Empty)
        {
            return;
        }

        // Start activity
        Activity? activity = EFCoreActivitySource.Source.StartActivity(
            EFCoreActivitySource.ActivityNames.Query,
            ActivityKind.Client);

        if (activity != null)
        {
            activity.SetTag(EFCoreActivitySource.TagNames.DbStatement, commandText);
            if (!string.IsNullOrEmpty(dbName))
            {
                activity.SetTag(EFCoreActivitySource.TagNames.DbName, dbName);
            }

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
        if (payload == null)
        {
            return;
        }

        Guid commandId = GetCommandId(payload);
        if (commandId == Guid.Empty)
        {
            return;
        }

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
        double durationMs = stopwatch?.Elapsed.TotalMilliseconds ?? 0;

        if (activity != null)
        {
            EFCoreActivitySource.SetDuration(activity, durationMs);
            EFCoreActivitySource.SetSuccess(activity);
            activity.Dispose();
        }

        // Record metrics
        string operation = GetOperation(payload);
        string? dbName = GetDatabaseName(payload);
        _metrics?.RecordQuery(durationMs, operation, dbName);
    }

    private void OnCommandError(object? payload)
    {
        if (payload == null)
        {
            return;
        }

        Guid commandId = GetCommandId(payload);
        if (commandId == Guid.Empty)
        {
            return;
        }

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

        Exception? exception = GetException(payload);
        if (activity != null)
        {
            if (exception != null)
            {
                EFCoreActivitySource.SetError(activity, exception);
            }

            activity.Dispose();
        }

        // Record error metrics
        string? dbName = GetDatabaseName(payload);
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
        Exception? exception = GetException(payload);
        _logger?.LogError(exception, "Database connection error");
    }

    #endregion

    #region Transaction Event Handlers

    private void OnTransactionStarted(object? payload)
    {
        string? dbName = GetDatabaseName(payload);
        _metrics?.RecordTransactionStart(dbName);
        _logger?.LogDebug("Transaction started on database {Database}", dbName);
    }

    private void OnTransactionCommitted(object? payload)
    {
        string? dbName = GetDatabaseName(payload);
        TimeSpan duration = GetDuration(payload);
        _metrics?.RecordTransactionCommit(duration.TotalMilliseconds, dbName);
        _logger?.LogDebug("Transaction committed on database {Database} after {DurationMs:F2}ms", dbName, duration.TotalMilliseconds);
    }

    private void OnTransactionRolledBack(object? payload)
    {
        string? dbName = GetDatabaseName(payload);
        TimeSpan duration = GetDuration(payload);
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
        if (payload == null)
        {
            return Guid.Empty;
        }

        Type type = payload.GetType();
        PropertyInfo? prop = type.GetProperty("CommandId");
        if (prop?.GetValue(payload) is Guid id)
        {
            return id;
        }

        return Guid.Empty;
    }

    private static string GetCommandText(object? payload)
    {
        if (payload == null)
        {
            return string.Empty;
        }

        Type type = payload.GetType();
        PropertyInfo? commandProp = type.GetProperty("Command");
        object? command = commandProp?.GetValue(payload);
        if (command == null)
        {
            return string.Empty;
        }

        PropertyInfo? textProp = command.GetType().GetProperty("CommandText");
        return textProp?.GetValue(command) as string ?? string.Empty;
    }

    private static string? GetDatabaseName(object? payload)
    {
        if (payload == null)
        {
            return null;
        }

        Type type = payload.GetType();

        // Try Connection property
        PropertyInfo? connectionProp = type.GetProperty("Connection");
        object? connection = connectionProp?.GetValue(payload);
        if (connection != null)
        {
            PropertyInfo? dbProp = connection.GetType().GetProperty("Database");
            return dbProp?.GetValue(connection) as string;
        }

        // Try DbContext property
        PropertyInfo? contextProp = type.GetProperty("Context");
        object? context = contextProp?.GetValue(payload);
        if (context != null)
        {
            PropertyInfo? databaseProp = context.GetType().GetProperty("Database");
            object? database = databaseProp?.GetValue(context);
            if (database != null)
            {
                PropertyInfo? currentDbProp = database.GetType().GetProperty("ProviderName");
                return currentDbProp?.GetValue(database) as string;
            }
        }

        return null;
    }

    private static Exception? GetException(object? payload)
    {
        if (payload == null)
        {
            return null;
        }

        Type type = payload.GetType();
        PropertyInfo? prop = type.GetProperty("Exception");
        return prop?.GetValue(payload) as Exception;
    }

    private static TimeSpan GetDuration(object? payload)
    {
        if (payload == null)
        {
            return TimeSpan.Zero;
        }

        Type type = payload.GetType();
        PropertyInfo? prop = type.GetProperty("Duration");
        if (prop?.GetValue(payload) is TimeSpan duration)
        {
            return duration;
        }

        return TimeSpan.Zero;
    }

    private static string GetOperation(object? payload)
    {
        string commandText = GetCommandText(payload);
        if (string.IsNullOrEmpty(commandText))
        {
            return "UNKNOWN";
        }

        string normalized = commandText.TrimStart().ToUpperInvariant();
        if (normalized.StartsWith("SELECT"))
        {
            return "SELECT";
        }

        if (normalized.StartsWith("INSERT"))
        {
            return "INSERT";
        }

        if (normalized.StartsWith("UPDATE"))
        {
            return "UPDATE";
        }

        if (normalized.StartsWith("DELETE"))
        {
            return "DELETE";
        }

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
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            foreach (Activity activity in _commandActivities.Values)
            {
                activity.Dispose();
            }
            _commandActivities.Clear();
            _commandTimings.Clear();
        }
    }
}

