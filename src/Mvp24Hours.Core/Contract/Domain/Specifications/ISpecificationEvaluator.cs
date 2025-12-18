//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Domain.Specifications;
using System.Linq;

namespace Mvp24Hours.Core.Contract.Domain.Specifications
{
    /// <summary>
    /// Interface for evaluating specifications and applying them to IQueryable.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <remarks>
    /// <para>
    /// This interface abstracts the specification evaluation process, allowing different
    /// implementations for different data access technologies (EF Core, MongoDB, etc.).
    /// </para>
    /// <para>
    /// <strong>Usage:</strong>
    /// The evaluator applies the specification's criteria, includes, ordering, and pagination
    /// to the source IQueryable, producing a new IQueryable that can be executed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerService
    /// {
    ///     private readonly ISpecificationEvaluator&lt;Customer&gt; _specEvaluator;
    ///     
    ///     public IList&lt;Customer&gt; GetActiveCustomers(IQueryable&lt;Customer&gt; source)
    ///     {
    ///         var spec = new ActiveCustomerSpecification();
    ///         return _specEvaluator.GetQuery(source, spec).ToList();
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface ISpecificationEvaluator<T>
        where T : class
    {
        /// <summary>
        /// Applies the specification to the query source.
        /// </summary>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        /// <remarks>
        /// <para>
        /// This method applies the specification's:
        /// <list type="bullet">
        /// <item>Criteria (Where clause) via <see cref="ISpecificationQuery{T}.IsSatisfiedByExpression"/></item>
        /// <item>Includes (navigation properties) if specification implements <see cref="ISpecificationQueryEnhanced{T}"/></item>
        /// <item>Ordering if specification implements <see cref="ISpecificationQueryEnhanced{T}"/></item>
        /// <item>Pagination (Skip/Take) if specification implements <see cref="ISpecificationQueryEnhanced{T}"/></item>
        /// </list>
        /// </para>
        /// </remarks>
        IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecificationQuery<T> specification);

        /// <summary>
        /// Applies the specification to the query source using the Specification base class.
        /// </summary>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        IQueryable<T> GetQuery(IQueryable<T> inputQuery, Specification<T> specification);
    }

    /// <summary>
    /// Non-generic interface for specification evaluation.
    /// </summary>
    public interface ISpecificationEvaluator
    {
        /// <summary>
        /// Gets a query with the specification applied.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecificationQuery<T> specification)
            where T : class;

        /// <summary>
        /// Gets a query with the specification applied.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, Specification<T> specification)
            where T : class;
    }
}

