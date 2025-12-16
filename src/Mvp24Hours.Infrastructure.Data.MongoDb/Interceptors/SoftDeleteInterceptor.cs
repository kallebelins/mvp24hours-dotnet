//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Infrastructure;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// MongoDB interceptor that converts physical deletes to soft deletes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interceptor intercepts delete operations and converts them to updates,
    /// setting the appropriate deletion fields instead of physically removing the document.
    /// </para>
    /// <para>
    /// Supports the following interfaces:
    /// <list type="bullet">
    ///   <item><see cref="ISoftDeletable"/> - Sets IsDeleted, DeletedAt, DeletedBy (string)</item>
    ///   <item><see cref="ISoftDeletable{TUserId}"/> - Sets IsDeleted, DeletedAt, DeletedBy (typed)</item>
    ///   <item><see cref="IEntityDateLog"/> - Sets Removed date (legacy)</item>
    ///   <item><see cref="IEntityLog{TForeignKey}"/> - Sets Removed and RemovedBy (legacy)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Important:</strong> You should configure query filters to exclude soft-deleted
    /// documents from normal queries. This can be done using aggregation pipelines or
    /// by applying filters in the repository.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI:
    /// services.AddScoped&lt;IMongoDbInterceptor, SoftDeleteInterceptor&gt;();
    /// 
    /// // Entity example:
    /// public class Product : ISoftDeletable
    /// {
    ///     public ObjectId Id { get; set; }
    ///     public string Name { get; set; }
    ///     public bool IsDeleted { get; set; }
    ///     public DateTime? DeletedAt { get; set; }
    ///     public string DeletedBy { get; set; }
    /// }
    /// </code>
    /// </example>
    public class SoftDeleteInterceptor : MongoDbInterceptorBase
    {
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IClock _clock;
        private readonly string _defaultUser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeleteInterceptor"/> class.
        /// </summary>
        /// <param name="currentUserProvider">Optional provider for the current user. If null, default user is used.</param>
        /// <param name="clock">Optional clock for getting current time. If null, UTC now is used.</param>
        /// <param name="defaultUser">Default user identifier when no user is available. Defaults to "System".</param>
        public SoftDeleteInterceptor(
            ICurrentUserProvider currentUserProvider = null,
            IClock clock = null,
            string defaultUser = "System")
        {
            _currentUserProvider = currentUserProvider;
            _clock = clock;
            _defaultUser = defaultUser ?? "System";
        }

        /// <inheritdoc />
        public override int Order => -900; // Run after audit interceptor

        /// <inheritdoc />
        public override Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            if (!IsSoftDeletable(entity))
            {
                // Not soft deletable, proceed with physical delete
                return Task.FromResult(DeleteInterceptionResult.Proceed());
            }

            var now = GetCurrentTime();
            var currentUser = GetCurrentUser();

            ApplySoftDelete(entity, now, currentUser);

            TelemetryHelper.Execute(TelemetryLevels.Verbose,
                $"mongodb-softdelete-interceptor-{typeof(T).Name}",
                new { EntityType = typeof(T).Name, User = currentUser, Timestamp = now });

            return Task.FromResult(DeleteInterceptionResult.SoftDelete());
        }

        /// <inheritdoc />
        public override Task OnAfterDeleteAsync<T>(T entity, bool wasSoftDeleted, CancellationToken cancellationToken = default)
        {
            if (wasSoftDeleted)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose,
                    $"mongodb-softdelete-completed-{typeof(T).Name}",
                    new { EntityType = typeof(T).Name, EntityId = entity.EntityKey });
            }

            return Task.CompletedTask;
        }

        private bool IsSoftDeletable<T>(T entity) where T : class
        {
            if (entity is ISoftDeletable)
                return true;

            if (entity is IEntityDateLog)
                return true;

            // Check for generic ISoftDeletable<T>
            var entityType = entity.GetType();
            foreach (var iface in entityType.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(ISoftDeletable<>))
                    return true;

                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEntityLog<>))
                    return true;
            }

            return false;
        }

        private void ApplySoftDelete<T>(T entity, DateTime now, string currentUser) where T : class
        {
            // Handle new ISoftDeletable interface
            if (entity is ISoftDeletable softDeletable)
            {
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = currentUser;
                return;
            }

            // Handle legacy IEntityDateLog
            if (entity is IEntityDateLog dateLog)
            {
                dateLog.Removed = now;
            }

            // Handle legacy IEntityLog<T> - set RemovedBy if possible
            TrySetSoftDeleteProperty(entity, "RemovedBy", currentUser, typeof(IEntityLog<>));

            // Handle generic ISoftDeletable<T>
            TrySetSoftDeleteProperty(entity, "IsDeleted", true, typeof(ISoftDeletable<>));
            TrySetSoftDeleteProperty(entity, "DeletedAt", now, typeof(ISoftDeletable<>));
            TrySetSoftDeleteProperty(entity, "DeletedBy", currentUser, typeof(ISoftDeletable<>));
        }

        private static void TrySetSoftDeleteProperty<T>(T entity, string propertyName, object value, Type genericInterfaceType)
            where T : class
        {
            var entityType = entity.GetType();
            var interfaces = entityType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericInterfaceType)
                {
                    var property = entityType.GetProperty(propertyName);
                    if (property != null && property.CanWrite)
                    {
                        try
                        {
                            var targetType = property.PropertyType;
                            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                            if (value != null && value.GetType() == underlyingType)
                            {
                                property.SetValue(entity, value);
                            }
                            else if (underlyingType == typeof(string) && value != null)
                            {
                                property.SetValue(entity, value.ToString());
                            }
                            else if (value != null)
                            {
                                var convertedValue = Convert.ChangeType(value, underlyingType);
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

        private DateTime GetCurrentTime()
        {
            return _clock?.UtcNow ?? TimeZoneHelper.GetTimeZoneNow();
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
}

