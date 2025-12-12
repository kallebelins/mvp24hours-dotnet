//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Repository interface combining standard repository operations with high-performance bulk operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface combines:
    /// <list type="bullet">
    /// <item><see cref="IRepositoryAsync{TEntity}"/> - Standard CRUD operations with change tracking</item>
    /// <item><see cref="IBulkOperationsAsync{TEntity}"/> - High-performance bulk operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// Use standard repository methods for small numbers of entities or when you need
    /// EF Core features like change tracking, events, and interceptors.
    /// </para>
    /// <para>
    /// Use bulk operations when:
    /// <list type="bullet">
    /// <item>Processing thousands of entities</item>
    /// <item>Performance is critical</item>
    /// <item>You don't need change tracking</item>
    /// <item>You don't need entity events/interceptors</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddScoped(typeof(IBulkOperationsRepositoryAsync&lt;&gt;), typeof(BulkOperationsRepositoryAsync&lt;&gt;));
    /// 
    /// // Use in service
    /// public class CustomerService
    /// {
    ///     private readonly IBulkOperationsRepositoryAsync&lt;Customer&gt; _repository;
    ///     
    ///     public async Task&lt;Customer&gt; GetByIdAsync(int id)
    ///     {
    ///         // Use standard repository method
    ///         return await _repository.GetByIdAsync(id);
    ///     }
    ///     
    ///     public async Task ImportCustomers(IList&lt;Customer&gt; customers)
    ///     {
    ///         // Use bulk operation for large dataset
    ///         await _repository.BulkInsertAsync(customers, new BulkOperationOptions
    ///         {
    ///             BatchSize = 5000
    ///         });
    ///     }
    ///     
    ///     public async Task DeactivateOldCustomers()
    ///     {
    ///         // Use ExecuteUpdate for condition-based updates
    ///         await _repository.ExecuteUpdateAsync(
    ///             c => c.LastOrderDate &lt; DateTime.UtcNow.AddYears(-2),
    ///             c => c.IsActive,
    ///             false
    ///         );
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IBulkOperationsRepositoryAsync<TEntity> : IRepositoryAsync<TEntity>, IBulkOperationsAsync<TEntity>
        where TEntity : IEntityBase
    {
    }
}

