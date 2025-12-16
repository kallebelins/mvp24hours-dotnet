//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Infrastructure.Data.MongoDb.Specifications;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for integrating Specification Pattern with MongoDB aggregation pipeline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions enable using specifications with MongoDB's native aggregation pipeline,
    /// providing more flexibility than IQueryable for complex queries like $lookup, $unwind,
    /// $group, and other aggregation stages.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Using specification with aggregation pipeline
    /// var spec = new ActiveCustomerSpecification();
    /// var customers = await collection.AggregateBySpecificationAsync(spec);
    /// 
    /// // With additional pipeline stages
    /// var pipeline = collection
    ///     .WithSpecification(spec)
    ///     .Lookup&lt;Customer, Order, CustomerWithOrders&gt;(
    ///         ordersCollection,
    ///         c => c.Id,
    ///         o => o.CustomerId,
    ///         c => c.Orders);
    /// var results = await pipeline.ToListAsync();
    /// </code>
    /// </example>
    public static class MongoDbSpecificationExtensions
    {
        #region [ Aggregation Pipeline Integration ]

        /// <summary>
        /// Creates an aggregation pipeline with the specification's filter applied as the first $match stage.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>An aggregation fluent interface with the specification's filter applied.</returns>
        /// <remarks>
        /// <para>
        /// This method converts the specification's expression to a MongoDB FilterDefinition
        /// and applies it as the first stage of an aggregation pipeline. You can then chain
        /// additional aggregation stages like $lookup, $project, $group, etc.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new PremiumCustomerSpecification();
        /// var pipeline = collection.WithSpecification(spec)
        ///     .Project(c => new { c.Name, c.Email, c.TotalSpent })
        ///     .Sort(Builders&lt;CustomerProjection&gt;.Sort.Descending(c => c.TotalSpent))
        ///     .Limit(10);
        /// 
        /// var topCustomers = await pipeline.ToListAsync();
        /// </code>
        /// </example>
        public static IAggregateFluent<T> WithSpecification<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification)
            where T : class
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (specification == null)
                throw new ArgumentNullException(nameof(specification));

            var filter = MongoDbSpecificationEvaluator.ToFilterDefinition(specification);
            var pipeline = collection.Aggregate().Match(filter);

            // Apply sorting if specification is enhanced
            if (specification is ISpecificationQueryEnhanced<T> enhancedSpec)
            {
                var sort = MongoDbSpecificationEvaluator.ToSortDefinition(enhancedSpec);
                if (sort != null)
                {
                    pipeline = pipeline.Sort(sort);
                }

                // Apply paging
                if (enhancedSpec.Skip.HasValue && enhancedSpec.Skip.Value > 0)
                {
                    pipeline = pipeline.Skip(enhancedSpec.Skip.Value);
                }

                if (enhancedSpec.Take.HasValue && enhancedSpec.Take.Value > 0)
                {
                    pipeline = pipeline.Limit(enhancedSpec.Take.Value);
                }
            }

            return pipeline;
        }

        /// <summary>
        /// Creates an aggregation pipeline with the specification's filter applied.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>An aggregation fluent interface with the specification's filter applied.</returns>
        public static IAggregateFluent<T> WithSpecification<T>(
            this IMongoCollection<T> collection,
            Specification<T> specification)
            where T : class
        {
            return collection.WithSpecification((ISpecificationQuery<T>)specification);
        }

        /// <summary>
        /// Executes an aggregation pipeline with the specification and returns the results.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of entities matching the specification.</returns>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var customers = await collection.AggregateBySpecificationAsync(spec);
        /// </code>
        /// </example>
        public static async Task<List<T>> AggregateBySpecificationAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var pipeline = collection.WithSpecification(specification);
            return await pipeline.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Executes an aggregation pipeline with the specification and returns a single result.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A single entity matching the specification, or null if not found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one entity matches.</exception>
        public static async Task<T?> AggregateSingleBySpecificationAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var pipeline = collection.WithSpecification(specification).Limit(2);
            var results = await pipeline.ToListAsync(cancellationToken);
            
            if (results.Count > 1)
            {
                throw new InvalidOperationException("Sequence contains more than one element.");
            }

            return results.Count > 0 ? results[0] : null;
        }

        /// <summary>
        /// Executes an aggregation pipeline with the specification and returns the first result.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The first entity matching the specification, or null if not found.</returns>
        public static async Task<T?> AggregateFirstBySpecificationAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var pipeline = collection.WithSpecification(specification).Limit(1);
            return await pipeline.FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Counts documents matching the specification using the aggregation pipeline.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The count of documents matching the specification.</returns>
        public static async Task<long> AggregateCountBySpecificationAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var filter = MongoDbSpecificationEvaluator.ToFilterDefinition(specification);
            return await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Checks if any documents match the specification.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>True if at least one document matches; otherwise, false.</returns>
        public static async Task<bool> AggregateAnyBySpecificationAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var pipeline = collection.WithSpecification(specification).Limit(1);
            var result = await pipeline.FirstOrDefaultAsync(cancellationToken);
            return result != null;
        }

        #endregion

        #region [ $lookup (Join) Support ]

        /// <summary>
        /// Performs a $lookup (left outer join) after applying the specification filter.
        /// </summary>
        /// <typeparam name="TSource">The source entity type.</typeparam>
        /// <typeparam name="TForeign">The foreign collection entity type.</typeparam>
        /// <typeparam name="TResult">The result type after the join.</typeparam>
        /// <param name="collection">The source MongoDB collection.</param>
        /// <param name="specification">The specification to apply to the source collection.</param>
        /// <param name="foreignCollectionName">The name of the foreign collection to join with.</param>
        /// <param name="localFieldName">The name of the local field.</param>
        /// <param name="foreignFieldName">The name of the foreign field.</param>
        /// <param name="asFieldName">The name for the result field containing joined documents.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of joined results as BsonDocuments.</returns>
        /// <remarks>
        /// <para>
        /// This method uses string-based field names for flexibility with the MongoDB aggregation pipeline.
        /// For strongly-typed lookups, consider using the MongoDB driver's aggregation fluent API directly.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var customersWithOrders = await collection.LookupBySpecificationAsync&lt;Customer, Order, BsonDocument&gt;(
        ///     spec,
        ///     "orders",
        ///     "_id",
        ///     "customerId",
        ///     "orders");
        /// </code>
        /// </example>
        public static async Task<List<BsonDocument>> LookupBySpecificationAsync<TSource, TForeign, TResult>(
            this IMongoCollection<TSource> collection,
            ISpecificationQuery<TSource> specification,
            string foreignCollectionName,
            string localFieldName,
            string foreignFieldName,
            string asFieldName,
            CancellationToken cancellationToken = default)
            where TSource : class
            where TForeign : class
            where TResult : class
        {
            var filter = MongoDbSpecificationEvaluator.ToFilterDefinition(specification);

            var lookupStage = new BsonDocument("$lookup", new BsonDocument
            {
                { "from", foreignCollectionName },
                { "localField", localFieldName },
                { "foreignField", foreignFieldName },
                { "as", asFieldName }
            });

            var pipeline = collection.Aggregate()
                .Match(filter)
                .AppendStage<BsonDocument>(lookupStage);

            return await pipeline.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Performs a $lookup (left outer join) with a custom pipeline after applying the specification filter.
        /// </summary>
        /// <typeparam name="T">The source entity type.</typeparam>
        /// <param name="collection">The source MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="lookupDefinition">The $lookup stage as a BsonDocument.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of results as BsonDocuments.</returns>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var lookupStage = new BsonDocument("$lookup", new BsonDocument
        /// {
        ///     { "from", "orders" },
        ///     { "let", new BsonDocument("customerId", "$_id") },
        ///     { "pipeline", new BsonArray
        ///         {
        ///             new BsonDocument("$match", new BsonDocument("$expr",
        ///                 new BsonDocument("$eq", new BsonArray { "$customerId", "$$customerId" })
        ///             ))
        ///         }
        ///     },
        ///     { "as", "orders" }
        /// });
        /// var results = await collection.LookupBySpecificationAsync(spec, lookupStage);
        /// </code>
        /// </example>
        public static async Task<List<BsonDocument>> LookupBySpecificationAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            BsonDocument lookupDefinition,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var filter = MongoDbSpecificationEvaluator.ToFilterDefinition(specification);

            var pipeline = collection.Aggregate()
                .Match(filter)
                .AppendStage<BsonDocument>(lookupDefinition);

            return await pipeline.ToListAsync(cancellationToken);
        }

        #endregion

        #region [ FilterDefinition Conversion ]

        /// <summary>
        /// Converts a specification to a MongoDB FilterDefinition.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="specification">The specification to convert.</param>
        /// <returns>A MongoDB FilterDefinition.</returns>
        /// <remarks>
        /// <para>
        /// This extension method provides a convenient way to convert any specification
        /// to a MongoDB filter for use with native MongoDB Driver operations.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var filter = spec.ToFilterDefinition();
        /// 
        /// // Use with native MongoDB operations
        /// var cursor = await collection.FindAsync(filter);
        /// await collection.DeleteManyAsync(filter);
        /// await collection.UpdateManyAsync(filter, update);
        /// </code>
        /// </example>
        public static FilterDefinition<T> ToFilterDefinition<T>(this ISpecificationQuery<T> specification)
            where T : class
        {
            return MongoDbSpecificationEvaluator.ToFilterDefinition(specification);
        }

        /// <summary>
        /// Converts a specification to a MongoDB SortDefinition.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="specification">The enhanced specification with ordering.</param>
        /// <returns>A MongoDB SortDefinition, or null if no ordering is specified.</returns>
        public static SortDefinition<T>? ToSortDefinition<T>(this ISpecificationQueryEnhanced<T> specification)
            where T : class
        {
            return MongoDbSpecificationEvaluator.ToSortDefinition(specification);
        }

        #endregion

        #region [ IEntityBase Extensions ]

        /// <summary>
        /// Creates an aggregation pipeline with the specification for entities implementing IEntityBase.
        /// </summary>
        /// <typeparam name="T">The entity type implementing IEntityBase.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <returns>An aggregation fluent interface with the specification's filter applied.</returns>
        public static IAggregateFluent<T> WithSpecificationForEntity<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification)
            where T : class, IEntityBase
        {
            return collection.WithSpecification(specification);
        }

        /// <summary>
        /// Executes aggregation and returns results for entities implementing IEntityBase.
        /// </summary>
        /// <typeparam name="T">The entity type implementing IEntityBase.</typeparam>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="specification">The specification to apply.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of entities matching the specification.</returns>
        public static Task<List<T>> AggregateBySpecificationForEntityAsync<T>(
            this IMongoCollection<T> collection,
            ISpecificationQuery<T> specification,
            CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            return collection.AggregateBySpecificationAsync(specification, cancellationToken);
        }

        #endregion

        #region [ Private Helpers ]

        /// <summary>
        /// Extracts the field name from an expression.
        /// </summary>
        private static string GetFieldName<T>(Expression<Func<T, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            return memberExpression?.Member.Name ?? expression.ToString();
        }

        #endregion
    }
}

