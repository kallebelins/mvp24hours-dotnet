//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Helpers;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors;

/// <summary>
/// MongoDB interceptor that automatically populates audit fields on entities.
/// </summary>
/// <remarks>
/// <para>
/// This interceptor automatically sets audit fields for entities implementing:
/// <list type="bullet">
///   <item><see cref="IAuditableEntity"/> - CreatedAt/CreatedBy and ModifiedAt/ModifiedBy with string user ID</item>
///   <item><see cref="IAuditableEntity{TUserId}"/> - Same as above with typed user ID</item>
///   <item><see cref="IEntityDateLog"/> - Created/Modified/Removed date tracking (legacy)</item>
///   <item><see cref="IEntityLog{TForeignKey}"/> - Date tracking with user ID (legacy)</item>
/// </list>
/// </para>
/// <para>
/// The current user is obtained from <see cref="ICurrentUserProvider"/> if available,
/// otherwise a default value ("System" or the configured default) is used.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI:
/// services.AddScoped&lt;IMongoDbInterceptor, AuditInterceptor&gt;();
/// 
/// // Or with custom configuration:
/// services.AddScoped&lt;IMongoDbInterceptor&gt;(sp =>
///     new AuditInterceptor(
///         sp.GetService&lt;ICurrentUserProvider&gt;(),
///         sp.GetService&lt;IClock&gt;(),
///         sp.GetService&lt;ILogger&lt;AuditInterceptor&gt;&gt;(),
///         "SystemUser"));
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the <see cref="AuditInterceptor"/> class.
/// </remarks>
/// <param name="currentUserProvider">Optional provider for the current user. If null, default user is used.</param>
/// <param name="clock">Optional clock for getting current time. If null, UTC now is used.</param>
/// <param name="logger">Optional logger for structured logging.</param>
/// <param name="defaultUser">Default user identifier when no user is available. Defaults to "System".</param>
public class AuditInterceptor(
    ICurrentUserProvider? currentUserProvider = null,
    IClock? clock = null,
    ILogger<AuditInterceptor>? logger = null,
    string defaultUser = "System") : MongoDbInterceptorBase
{
    private readonly ICurrentUserProvider? _currentUserProvider = currentUserProvider;
    private readonly IClock? _clock = clock;
    private readonly ILogger<AuditInterceptor>? _logger = logger;
    private readonly string _defaultUser = defaultUser ?? "System";

    /// <inheritdoc />
    public override int Order => -1000; // Run early in the pipeline

    /// <inheritdoc />
    public override Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        DateTime now = GetCurrentTime();
        string currentUser = GetCurrentUser();

        ApplyCreateAudit(entity, now, currentUser);

        _logger?.LogDebug("Applying create audit fields for entity {EntityType} (Id: {EntityId}) by user {User} at {Timestamp}",
            typeof(T).Name, entity.EntityKey, currentUser, now);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task OnBeforeUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
    {
        DateTime now = GetCurrentTime();
        string currentUser = GetCurrentUser();

        ApplyUpdateAudit(entity, now, currentUser);

        _logger?.LogDebug("Applying update audit fields for entity {EntityType} (Id: {EntityId}) by user {User} at {Timestamp}",
            typeof(T).Name, entity.EntityKey, currentUser, now);

        return Task.CompletedTask;
    }

    private void ApplyCreateAudit<T>(T entity, DateTime now, string currentUser) where T : class
    {
        // Handle IAuditableEntity (new standard interface)
        if (entity is IAuditableEntity auditable)
        {
            auditable.CreatedAt = now;
            auditable.CreatedBy = currentUser;
            auditable.ModifiedAt = null;
            auditable.ModifiedBy = null;
        }

        // Handle legacy IEntityDateLog
        if (entity is IEntityDateLog dateLog)
        {
            dateLog.Created = now;
            dateLog.Modified = null;
            dateLog.Removed = null;
        }

        // Handle legacy IEntityLog<T> - set CreatedBy if possible
        TrySetGenericAuditProperty(entity, "CreatedBy", currentUser, typeof(IEntityLog<>));
    }

    private void ApplyUpdateAudit<T>(T entity, DateTime now, string currentUser) where T : class
    {
        // Handle IAuditableEntity (new standard interface)
        if (entity is IAuditableEntity auditable)
        {
            auditable.ModifiedAt = now;
            auditable.ModifiedBy = currentUser;
            // Note: Created fields should be preserved from the original entity
        }

        // Handle legacy IEntityDateLog
        if (entity is IEntityDateLog dateLog)
        {
            dateLog.Modified = now;
            // Note: Created field should be preserved from the original entity
        }

        // Handle legacy IEntityLog<T> - set ModifiedBy if possible
        TrySetGenericAuditProperty(entity, "ModifiedBy", currentUser, typeof(IEntityLog<>));
    }

    private static void TrySetGenericAuditProperty<T>(T entity, string propertyName, object value, Type genericInterfaceType)
        where T : class
    {
        Type entityType = entity.GetType();
        Type[] interfaces = entityType.GetInterfaces();

        foreach (Type iface in interfaces)
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericInterfaceType)
            {
                PropertyInfo? property = entityType.GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        // Try to convert value to the expected type
                        Type targetType = property.PropertyType;
                        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                        if (underlyingType == typeof(string))
                        {
                            property.SetValue(entity, value?.ToString());
                        }
                        else if (value != null)
                        {
                            object convertedValue = Convert.ChangeType(value, underlyingType);
                            property.SetValue(entity, convertedValue);
                        }
                    }
                    catch
                    {
                        // Silently ignore if conversion fails
                    }
                }
                break;
            }
        }
    }

    /// <remarks>
    /// When no <c>IClock</c> is registered the fallback keeps the legacy <c>TimeZoneHelper</c>
    /// behavior, which returns South America local time while <c>IClock.UtcNow</c> returns UTC —
    /// so the stamped value depends on whether the clock was registered. Register an
    /// <c>IClock</c> (e.g. <c>services.AddTimeProvider()</c>) to get a consistent, injectable time
    /// source. The fallback is kept as-is to avoid changing behavior for existing consumers.
    /// </remarks>
    private DateTime GetCurrentTime()
    {
        // TODO (task 4.2b): drop the TimeZoneHelper fallback when the helper is removed in v12.
#pragma warning disable CS0618 // intentional: legacy fallback preserved until removal in v12
        return _clock?.UtcNow ?? TimeZoneHelper.GetTimeZoneNow();
#pragma warning restore CS0618
    }

    private string GetCurrentUser()
    {
        if (_currentUserProvider != null && !string.IsNullOrEmpty(_currentUserProvider.UserId))
        {
            return _currentUserProvider.UserId;
        }
        return _defaultUser;
    }
}

