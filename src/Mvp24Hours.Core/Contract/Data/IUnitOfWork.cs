//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System;
using System.Data;
using System.Threading;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Implements the Unit of Work pattern to maintain consistency and coordinate transactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Design Pattern: Unit of Work</strong>
    /// </para>
    /// <para>
    /// Maintains a list of objects affected by a business transaction and coordinates
    /// writing out changes and resolving concurrency issues. This pattern ensures that
    /// all changes are committed together or not at all, maintaining data consistency.
    /// </para>
    /// <para>
    /// <strong>Key Benefits:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>Coordinates multiple repository operations in a single transaction</item>
    /// <item>Reduces database round trips by batching operations</item>
    /// <item>Ensures data consistency through ACID transactions</item>
    /// <item>Provides rollback capability for failed operations</item>
    /// <item>Manages database connection lifecycle</item>
    /// </list>
    /// <para>
    /// Learn more: http://martinfowler.com/eaaCatalog/unitOfWork.html
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using (var unitOfWork = serviceProvider.GetService&lt;IUnitOfWork&gt;())
    /// {
    ///     try
    ///     {
    ///         var customerRepo = unitOfWork.GetRepository&lt;Customer&gt;();
    ///         var orderRepo = unitOfWork.GetRepository&lt;Order&gt;();
    ///         
    ///         // Multiple operations in the same transaction
    ///         customerRepo.Add(newCustomer);
    ///         orderRepo.Add(newOrder);
    ///         
    ///         // Commit all changes atomically
    ///         unitOfWork.SaveChanges();
    ///     }
    ///     catch (Exception)
    ///     {
    ///         // Discard all changes on error
    ///         unitOfWork.Rollback();
    ///         throw;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Persists all changes made in the current transaction to the database.
        /// </summary>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the save operation.
        /// If cancellation is requested, the operation is rolled back automatically.
        /// </param>
        /// <returns>
        /// The number of state entries written to the database. This can include
        /// inserted, updated, and deleted records.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method writes all pending changes (adds, modifications, and deletions) to the
        /// database in a single transaction. If any operation fails, all changes are rolled back
        /// automatically, ensuring data consistency.
        /// </para>
        /// <para>
        /// <strong>Cancellation Handling:</strong> If the cancellation token is triggered,
        /// the method calls <see cref="Rollback"/> automatically and returns 0.
        /// </para>
        /// <para>
        /// <strong>Performance Tip:</strong> Batch multiple operations together before calling
        /// SaveChanges to reduce database round trips and improve performance.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Simple usage
        /// repository.Add(customer);
        /// int changedRecords = unitOfWork.SaveChanges();
        /// 
        /// // With cancellation
        /// var cts = new CancellationTokenSource();
        /// int changedRecords = unitOfWork.SaveChanges(cts.Token);
        /// </code>
        /// </example>
        int SaveChanges(CancellationToken cancellationToken = default);

        /// <summary>
        /// Discards all pending changes in the current transaction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method reverts all uncommitted changes to their original state, including:
        /// </para>
        /// <list type="bullet">
        /// <item><strong>Modified entities:</strong> Reset to their original values</item>
        /// <item><strong>Added entities:</strong> Detached from the context</item>
        /// <item><strong>Deleted entities:</strong> Restored to unchanged state</item>
        /// </list>
        /// <para>
        /// Use this method when you need to cancel a transaction or recover from an error
        /// without closing the unit of work.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// try
        /// {
        ///     repository.Add(customer);
        ///     if (!ValidateBusinessRules(customer))
        ///     {
        ///         unitOfWork.Rollback();
        ///         return;
        ///     }
        ///     unitOfWork.SaveChanges();
        /// }
        /// catch (Exception)
        /// {
        ///     unitOfWork.Rollback();
        ///     throw;
        /// }
        /// </code>
        /// </example>
        void Rollback();

        /// <summary>
        /// Gets a repository instance for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The type of entity, which must implement <see cref="IEntityBase"/>.</typeparam>
        /// <returns>
        /// A repository instance for managing entities of type <typeparamref name="T"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method provides access to entity-specific repositories while ensuring they all
        /// participate in the same transaction. Repositories obtained through this method share
        /// the same database context and transaction scope.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> The same repository instance is returned for multiple calls
        /// with the same entity type, ensuring consistency within the unit of work.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customerRepo = unitOfWork.GetRepository&lt;Customer&gt;();
        /// var orderRepo = unitOfWork.GetRepository&lt;Order&gt;();
        /// 
        /// // Both repositories share the same transaction
        /// customerRepo.Add(newCustomer);
        /// orderRepo.Add(newOrder);
        /// unitOfWork.SaveChanges(); // Both changes committed together
        /// </code>
        /// </example>
        IRepository<T> GetRepository<T>() where T : class, IEntityBase;

        /// <summary>
        /// Gets the underlying database connection for advanced scenarios.
        /// </summary>
        /// <returns>
        /// The <see cref="IDbConnection"/> instance used by this unit of work,
        /// or <c>null</c> if no connection is available.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Use this method when you need to execute raw SQL commands or stored procedures
        /// that aren't covered by the repository pattern. The connection is managed by the
        /// unit of work and should not be disposed directly.
        /// </para>
        /// <para>
        /// <strong>Warning:</strong> Operations performed directly on the connection bypass
        /// the change tracking mechanism. Ensure changes are compatible with any pending
        /// repository operations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using var connection = unitOfWork.GetConnection();
        /// using var command = connection.CreateCommand();
        /// command.CommandText = "SELECT COUNT(*) FROM Customers WHERE IsActive = 1";
        /// var count = (int)command.ExecuteScalar();
        /// </code>
        /// </example>
        IDbConnection GetConnection();
    }
}
