//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// Base class for MongoDB interceptors providing default no-op implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inherit from this class and override only the methods you need.
    /// All methods have default no-op implementations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class LoggingInterceptor : MongoDbInterceptorBase
    /// {
    ///     public override Task OnBeforeInsertAsync&lt;T&gt;(T entity, CancellationToken ct)
    ///     {
    ///         _logger.LogInformation("Inserting {Type}", typeof(T).Name);
    ///         return Task.CompletedTask;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class MongoDbInterceptorBase : IMongoDbInterceptor
    {
        /// <inheritdoc />
        public virtual int Order => 0;

        /// <inheritdoc />
        public virtual Task OnBeforeInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task OnAfterInsertAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task OnBeforeUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task OnAfterUpdateAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual Task<DeleteInterceptionResult> OnBeforeDeleteAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.FromResult(DeleteInterceptionResult.Proceed());
        }

        /// <inheritdoc />
        public virtual Task OnAfterDeleteAsync<T>(T entity, bool wasSoftDeleted, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return Task.CompletedTask;
        }
    }
}

