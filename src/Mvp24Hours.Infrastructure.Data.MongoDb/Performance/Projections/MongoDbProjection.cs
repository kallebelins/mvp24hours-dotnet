//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Projections
{
    /// <summary>
    /// Provides methods for building MongoDB projection definitions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Projections allow you to return only specific fields from documents,
    /// reducing network bandwidth and improving query performance.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Include specific fields
    /// var projection = MongoDbProjection&lt;Customer&gt;.Include(c => c.Name, c => c.Email);
    /// 
    /// // Exclude specific fields
    /// var projection = MongoDbProjection&lt;Customer&gt;.Exclude(c => c.Password, c => c.InternalNotes);
    /// 
    /// // Project to a DTO
    /// var options = new MongoDbProjectionOptions&lt;Customer, CustomerDto&gt;();
    /// var projection = options.Build();
    /// </code>
    /// </example>
    public static class MongoDbProjection<T>
    {
        /// <summary>
        /// Creates a projection that includes only the specified fields.
        /// </summary>
        /// <param name="fields">The fields to include.</param>
        /// <returns>A projection definition.</returns>
        /// <example>
        /// <code>
        /// var projection = MongoDbProjection&lt;Customer&gt;.Include(c => c.Name, c => c.Email);
        /// var result = await collection.Find(filter).Project(projection).ToListAsync();
        /// </code>
        /// </example>
        public static ProjectionDefinition<T> Include(params Expression<Func<T, object>>[] fields)
        {
            if (fields == null || fields.Length == 0)
            {
                return Builders<T>.Projection.Combine();
            }

            var projections = fields.Select(f => Builders<T>.Projection.Include(f)).ToList();
            return Builders<T>.Projection.Combine(projections);
        }

        /// <summary>
        /// Creates a projection that includes only the specified field names.
        /// </summary>
        /// <param name="fieldNames">The field names to include.</param>
        /// <returns>A projection definition.</returns>
        public static ProjectionDefinition<T> Include(params string[] fieldNames)
        {
            if (fieldNames == null || fieldNames.Length == 0)
            {
                return Builders<T>.Projection.Combine();
            }

            var projections = fieldNames.Select(f => Builders<T>.Projection.Include(f)).ToList();
            return Builders<T>.Projection.Combine(projections);
        }

        /// <summary>
        /// Creates a projection that excludes the specified fields.
        /// </summary>
        /// <param name="fields">The fields to exclude.</param>
        /// <returns>A projection definition.</returns>
        /// <example>
        /// <code>
        /// var projection = MongoDbProjection&lt;Customer&gt;.Exclude(c => c.Password);
        /// var result = await collection.Find(filter).Project(projection).ToListAsync();
        /// </code>
        /// </example>
        public static ProjectionDefinition<T> Exclude(params Expression<Func<T, object>>[] fields)
        {
            if (fields == null || fields.Length == 0)
            {
                return Builders<T>.Projection.Combine();
            }

            var projections = fields.Select(f => Builders<T>.Projection.Exclude(f)).ToList();
            return Builders<T>.Projection.Combine(projections);
        }

        /// <summary>
        /// Creates a projection that excludes the specified field names.
        /// </summary>
        /// <param name="fieldNames">The field names to exclude.</param>
        /// <returns>A projection definition.</returns>
        public static ProjectionDefinition<T> Exclude(params string[] fieldNames)
        {
            if (fieldNames == null || fieldNames.Length == 0)
            {
                return Builders<T>.Projection.Combine();
            }

            var projections = fieldNames.Select(f => Builders<T>.Projection.Exclude(f)).ToList();
            return Builders<T>.Projection.Combine(projections);
        }

        /// <summary>
        /// Creates a projection from an expression.
        /// </summary>
        /// <typeparam name="TResult">The projection result type.</typeparam>
        /// <param name="expression">The projection expression.</param>
        /// <returns>A projection definition.</returns>
        /// <example>
        /// <code>
        /// var projection = MongoDbProjection&lt;Customer&gt;.Expression&lt;CustomerDto&gt;(
        ///     c => new CustomerDto { Name = c.Name, Email = c.Email });
        /// </code>
        /// </example>
        public static ProjectionDefinition<T, TResult> Expression<TResult>(Expression<Func<T, TResult>> expression)
        {
            return Builders<T>.Projection.Expression(expression);
        }

        /// <summary>
        /// Creates a slice projection for array fields.
        /// </summary>
        /// <param name="field">The array field.</param>
        /// <param name="limit">The number of elements to return.</param>
        /// <returns>A projection definition.</returns>
        /// <example>
        /// <code>
        /// // Get only the first 5 orders
        /// var projection = MongoDbProjection&lt;Customer&gt;.Slice(c => c.Orders, 5);
        /// </code>
        /// </example>
        public static ProjectionDefinition<T> Slice(Expression<Func<T, object>> field, int limit)
        {
            return Builders<T>.Projection.Slice(field, limit);
        }

        /// <summary>
        /// Creates a slice projection for array fields with skip and limit.
        /// </summary>
        /// <param name="field">The array field.</param>
        /// <param name="skip">The number of elements to skip.</param>
        /// <param name="limit">The number of elements to return.</param>
        /// <returns>A projection definition.</returns>
        public static ProjectionDefinition<T> Slice(Expression<Func<T, object>> field, int skip, int limit)
        {
            return Builders<T>.Projection.Slice(field, skip, limit);
        }

        /// <summary>
        /// Creates an element match projection for array fields.
        /// </summary>
        /// <typeparam name="TItem">The array element type.</typeparam>
        /// <param name="field">The array field.</param>
        /// <param name="filter">The filter for matching elements.</param>
        /// <returns>A projection definition.</returns>
        public static ProjectionDefinition<T> ElemMatch<TItem>(
            Expression<Func<T, IEnumerable<TItem>>> field,
            FilterDefinition<TItem> filter)
        {
            return Builders<T>.Projection.ElemMatch(field, filter);
        }

        /// <summary>
        /// Creates a meta text score projection.
        /// </summary>
        /// <param name="fieldName">The field name to store the score.</param>
        /// <returns>A projection definition.</returns>
        /// <remarks>
        /// Use with text search queries to include the text search score in results.
        /// </remarks>
        public static ProjectionDefinition<T> MetaTextScore(string fieldName = "score")
        {
            return Builders<T>.Projection.MetaTextScore(fieldName);
        }
    }

    /// <summary>
    /// Provides options for building typed projection definitions.
    /// </summary>
    /// <typeparam name="TSource">The source document type.</typeparam>
    /// <typeparam name="TDestination">The destination/projected type.</typeparam>
    public class MongoDbProjectionOptions<TSource, TDestination>
    {
        private readonly List<string> _includeFields = new();
        private readonly List<string> _excludeFields = new();
        private Expression<Func<TSource, TDestination>> _projectionExpression;

        /// <summary>
        /// Includes the specified fields in the projection.
        /// </summary>
        /// <param name="fields">The fields to include.</param>
        /// <returns>The options instance for chaining.</returns>
        public MongoDbProjectionOptions<TSource, TDestination> Include(
            params Expression<Func<TSource, object>>[] fields)
        {
            foreach (var field in fields)
            {
                _includeFields.Add(GetFieldName(field));
            }
            return this;
        }

        /// <summary>
        /// Excludes the specified fields from the projection.
        /// </summary>
        /// <param name="fields">The fields to exclude.</param>
        /// <returns>The options instance for chaining.</returns>
        public MongoDbProjectionOptions<TSource, TDestination> Exclude(
            params Expression<Func<TSource, object>>[] fields)
        {
            foreach (var field in fields)
            {
                _excludeFields.Add(GetFieldName(field));
            }
            return this;
        }

        /// <summary>
        /// Sets a projection expression for transforming results.
        /// </summary>
        /// <param name="expression">The projection expression.</param>
        /// <returns>The options instance for chaining.</returns>
        public MongoDbProjectionOptions<TSource, TDestination> Project(
            Expression<Func<TSource, TDestination>> expression)
        {
            _projectionExpression = expression;
            return this;
        }

        /// <summary>
        /// Automatically maps fields from source to destination based on matching property names.
        /// </summary>
        /// <returns>The options instance for chaining.</returns>
        public MongoDbProjectionOptions<TSource, TDestination> AutoMap()
        {
            var destProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .Select(p => p.Name)
                .ToList();

            var sourceProperties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => destProperties.Contains(p.Name))
                .ToList();

            foreach (var prop in sourceProperties)
            {
                var bsonElement = prop.GetCustomAttribute<BsonElementAttribute>();
                var fieldName = bsonElement?.ElementName ?? prop.Name;
                _includeFields.Add(fieldName);
            }

            return this;
        }

        /// <summary>
        /// Builds the projection definition.
        /// </summary>
        /// <returns>The projection definition.</returns>
        public ProjectionDefinition<TSource, TDestination> Build()
        {
            if (_projectionExpression != null)
            {
                return Builders<TSource>.Projection.Expression(_projectionExpression);
            }

            // When using include/exclude fields, we need to use the Expression projection
            // The caller should provide an expression for proper type mapping
            throw new InvalidOperationException(
                "When using Include/Exclude fields, you must call Project() with an expression to define the mapping to the destination type. " +
                "Example: options.Project(x => new TDestination { ... })");
        }

        /// <summary>
        /// Builds a projection definition for the source type only (without type conversion).
        /// </summary>
        /// <returns>The projection definition.</returns>
        public ProjectionDefinition<TSource> BuildSourceProjection()
        {
            var projections = new List<ProjectionDefinition<TSource>>();

            foreach (var field in _includeFields)
            {
                projections.Add(Builders<TSource>.Projection.Include(field));
            }

            foreach (var field in _excludeFields)
            {
                projections.Add(Builders<TSource>.Projection.Exclude(field));
            }

            if (projections.Count == 0)
            {
                return Builders<TSource>.Projection.Combine();
            }

            return Builders<TSource>.Projection.Combine(projections);
        }

        private static string GetFieldName(Expression<Func<TSource, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            if (memberExpression?.Member is PropertyInfo propertyInfo)
            {
                var bsonElement = propertyInfo.GetCustomAttribute<BsonElementAttribute>();
                return bsonElement?.ElementName ?? propertyInfo.Name;
            }

            return expression.ToString();
        }
    }
}

