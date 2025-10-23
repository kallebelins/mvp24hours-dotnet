//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Data
{
    /// <summary>
    /// Provides command operations for modifying database entities (add, modify, delete).
    /// </summary>
    /// <typeparam name="TEntity">The type of entity that implements <see cref="IEntityBase"/>.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface defines the contract for write operations on entities in a data store.
    /// It follows the Command Query Responsibility Segregation (CQRS) pattern by separating
    /// write operations from read operations (see <see cref="IQuery{TEntity}"/>).
    /// </para>
    /// <para>
    /// <strong>Important:</strong> Changes made through these methods are not immediately persisted
    /// to the database. You must call <see cref="IUnitOfWork.SaveChanges"/> to commit the changes.
    /// </para>
    /// <para>
    /// <strong>Soft Delete:</strong> The Remove methods perform logical deletion by setting
    /// removed date/user fields on entities that implement <see cref="Core.Contract.Domain.Entity.IEntityLog{T}"/>
    /// or <see cref="Core.Contract.Domain.Entity.IEntityDateLog"/>. For entities without these interfaces,
    /// physical deletion is performed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerRepository : ICommand&lt;Customer&gt;
    /// {
    ///     private readonly DbContext context;
    ///     
    ///     public void Add(Customer entity)
    ///     {
    ///         context.Customers.Add(entity);
    ///     }
    /// }
    /// 
    /// // Usage with Unit of Work
    /// var customer = new Customer { Name = "John Doe" };
    /// repository.Add(customer);
    /// unitOfWork.SaveChanges(); // Changes are persisted here
    /// </code>
    /// </example>
    public interface ICommand<TEntity>
        where TEntity : IEntityBase
    {
        /// <summary>
        /// Adds a new entity to the data store.
        /// </summary>
        /// <param name="entity">The entity instance to add.</param>
        /// <remarks>
        /// <para>
        /// This method marks the entity for insertion. The actual database insert occurs when
        /// <see cref="IUnitOfWork.SaveChanges"/> is called.
        /// </para>
        /// <para>
        /// If the entity is null, this method does nothing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customer = new Customer 
        /// { 
        ///     Name = "John Doe", 
        ///     Email = "john@example.com" 
        /// };
        /// repository.Add(customer);
        /// unitOfWork.SaveChanges();
        /// // customer.Id now contains the generated database ID
        /// </code>
        /// </example>
        void Add(TEntity entity);

        /// <summary>
        /// Adds multiple entities to the data store in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities to add.</param>
        /// <remarks>
        /// <para>
        /// This method is more efficient than calling <see cref="Add(TEntity)"/> multiple times
        /// as it can be optimized by the underlying data access provider.
        /// </para>
        /// <para>
        /// If the collection is null or empty, this method does nothing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customers = new List&lt;Customer&gt;
        /// {
        ///     new Customer { Name = "John Doe" },
        ///     new Customer { Name = "Jane Smith" }
        /// };
        /// repository.Add(customers);
        /// unitOfWork.SaveChanges();
        /// </code>
        /// </example>
        void Add(IList<TEntity> entities);

        /// <summary>
        /// Updates an existing entity in the data store.
        /// </summary>
        /// <param name="entity">The entity instance with updated values.</param>
        /// <remarks>
        /// <para>
        /// This method marks the entity for update. The actual database update occurs when
        /// <see cref="IUnitOfWork.SaveChanges"/> is called.
        /// </para>
        /// <para>
        /// The entity must exist in the database (matched by its primary key). An exception
        /// is thrown if the entity is not found.
        /// </para>
        /// <para>
        /// For entities implementing <see cref="Core.Contract.Domain.Entity.IEntityLog{T}"/>,
        /// audit fields (Created, CreatedBy) are preserved and not overwritten.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customer = repository.GetById(1);
        /// customer.Name = "Updated Name";
        /// repository.Modify(customer);
        /// unitOfWork.SaveChanges();
        /// </code>
        /// </example>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the entity with the specified key is not found in the database.
        /// </exception>
        void Modify(TEntity entity);

        /// <summary>
        /// Updates multiple entities in the data store in a single operation.
        /// </summary>
        /// <param name="entities">The collection of entities with updated values.</param>
        /// <remarks>
        /// <para>
        /// This method is more efficient than calling <see cref="Modify(TEntity)"/> multiple times.
        /// </para>
        /// <para>
        /// All entities must exist in the database. If any entity is not found, an exception is thrown.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customers = repository.GetBy(c => c.City == "New York").ToList();
        /// foreach (var customer in customers)
        /// {
        ///     customer.Status = "Verified";
        /// }
        /// repository.Modify(customers);
        /// unitOfWork.SaveChanges();
        /// </code>
        /// </example>
        void Modify(IList<TEntity> entities);

        /// <summary>
        /// Removes an entity from the data store (logical deletion for auditable entities).
        /// </summary>
        /// <param name="entity">The entity instance to remove.</param>
        /// <remarks>
        /// <para>
        /// <strong>Logical Deletion:</strong> If the entity implements <see cref="Core.Contract.Domain.Entity.IEntityLog{T}"/>
        /// or <see cref="Core.Contract.Domain.Entity.IEntityDateLog"/>, this method performs a soft delete by
        /// setting the Removed and RemovedBy fields. The record remains in the database but is marked as deleted.
        /// </para>
        /// <para>
        /// <strong>Physical Deletion:</strong> For entities without audit interfaces, this method performs
        /// a hard delete, permanently removing the record from the database.
        /// </para>
        /// <para>
        /// If the entity is null, this method does nothing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customer = repository.GetById(1);
        /// repository.Remove(customer);
        /// unitOfWork.SaveChanges();
        /// // If Customer implements IEntityLog, it's soft deleted
        /// // Otherwise, it's permanently removed
        /// </code>
        /// </example>
        void Remove(TEntity entity);

        /// <summary>
        /// Removes multiple entities from the data store (logical deletion for auditable entities).
        /// </summary>
        /// <param name="entities">The collection of entities to remove.</param>
        /// <remarks>
        /// See <see cref="Remove(TEntity)"/> for details on soft vs. hard deletion.
        /// </remarks>
        /// <example>
        /// <code>
        /// var inactiveCustomers = repository.GetBy(c => !c.IsActive).ToList();
        /// repository.Remove(inactiveCustomers);
        /// unitOfWork.SaveChanges();
        /// </code>
        /// </example>
        void Remove(IList<TEntity> entities);

        /// <summary>
        /// Removes an entity by its unique identifier (logical deletion for auditable entities).
        /// </summary>
        /// <param name="id">The unique identifier of the entity to remove.</param>
        /// <remarks>
        /// <para>
        /// This method retrieves the entity by ID and then calls <see cref="Remove(TEntity)"/>.
        /// See <see cref="Remove(TEntity)"/> for details on soft vs. hard deletion.
        /// </para>
        /// <para>
        /// If no entity with the specified ID exists, this method does nothing.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// repository.RemoveById(123);
        /// unitOfWork.SaveChanges();
        /// </code>
        /// </example>
        void RemoveById(object id);

        /// <summary>
        /// Removes multiple entities by their unique identifiers (logical deletion for auditable entities).
        /// </summary>
        /// <param name="ids">The collection of entity identifiers to remove.</param>
        /// <remarks>
        /// This method retrieves each entity by ID and then removes it. See <see cref="Remove(TEntity)"/>
        /// for details on soft vs. hard deletion.
        /// </remarks>
        /// <example>
        /// <code>
        /// var idsToDelete = new List&lt;object&gt; { 1, 2, 3, 4, 5 };
        /// repository.RemoveById(idsToDelete);
        /// unitOfWork.SaveChanges();
        /// </code>
        /// </example>
        void RemoveById(IList<object> ids);
    }
}
