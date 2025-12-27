//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Base
{
    /// <summary>
    ///  <see cref="Core.Contract.Data.IRepository"/>
    /// </summary>
    public abstract class RepositoryBase<T>
        where T : class, IEntityBase
    {
        #region [ Ctor ]

        protected RepositoryBase(Mvp24HoursContext dbContext, IOptions<MongoDbRepositoryOptions> options, ILogger<RepositoryBase<T>>? logger = null)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            dbEntities = dbContext.Set<T>();
            this.Options = options?.Value ?? new MongoDbRepositoryOptions();
            _logger = logger;
        }

        #endregion

        #region [ Fields ]

        /// <summary>
        /// Database context
        /// </summary>
        protected readonly Mvp24HoursContext dbContext;
        /// <summary>
        /// Represents relationship with entities in the database
        /// </summary>
        protected IMongoCollection<T> dbEntities;
        /// <summary>
        /// Gets the value of the user logged in the context or logged into the database
        /// </summary>
        protected abstract object EntityLogBy { get; }
        /// <summary>
        /// Repository configuration options
        /// </summary>
        protected MongoDbRepositoryOptions Options { get; private set; }
        /// <summary>
        /// Logger instance
        /// </summary>
        protected readonly ILogger<RepositoryBase<T>>? _logger;

        #endregion

        #region [ Methods ]
        /// <summary>
        /// Gets database query with clause and aggregation of relationships
        /// </summary>
        protected IQueryable<T> GetQuery(IPagingCriteria criteria, bool onlyNavigation = false)
        {
            // cria query
            var query = dbEntities.AsQueryable();
            return GetQuery(query, criteria, onlyNavigation);
        }
        /// <summary>
        /// Gets database query with clause and aggregation of relationships
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        protected IQueryable<T> GetQuery(IQueryable<T> query, IPagingCriteria criteria, bool onlyNavigation = false)
        {
            _logger?.LogDebug("Building query with criteria. Type: {CriteriaType}", criteria?.GetType().Name);
            var ordered = false;

            if (!onlyNavigation)
            {
                int offset = 0;
                int limit = Options.MaxQtyByQueryPage;

                if (criteria != null)
                {
                    // ordination
                    if (criteria is IPagingCriteriaExpression<T>)
                    {
                        var clauseExpr = criteria as IPagingCriteriaExpression<T>;
                        // ordination by ascending expression
                        if (clauseExpr.OrderByAscendingExpr.AnySafe())
                        {
                            IOrderedQueryable<T> queryOrdered = null;
                            foreach (var ord in clauseExpr.OrderByAscendingExpr)
                            {
                                if (queryOrdered == null)
                                {
                                    ordered = true;
                                    queryOrdered = query.OrderBy(ord);
                                }
                                else
                                {
                                    queryOrdered = queryOrdered.ThenBy(ord);
                                }
                            }
                            query = queryOrdered ?? query;
                        }

                        // ordination by descending expression
                        if (clauseExpr.OrderByDescendingExpr.AnySafe())
                        {
                            IOrderedQueryable<T> queryOrdered = null;
                            foreach (var ord in clauseExpr.OrderByDescendingExpr)
                            {
                                if (queryOrdered == null)
                                {
                                    ordered = true;
                                    queryOrdered = query.OrderByDescending(ord);
                                }
                                else
                                {
                                    queryOrdered = queryOrdered.ThenByDescending(ord);
                                }
                            }
                            query = queryOrdered ?? query;
                        }
                    }

                    // ordination by string
                    if (criteria.OrderBy.AnySafe())
                    {
                        IOrderedQueryable<T> queryOrdered = null;
                        foreach (var ord in criteria.OrderBy)
                        {
                            if (queryOrdered == null)
                            {
                                ordered = true;
                                queryOrdered = query.OrderBy(ord);
                            }
                            else
                            {
                                queryOrdered = queryOrdered.ThenBy(ord);
                            }
                        }
                        query = queryOrdered ?? query;
                    }

                    // Paging
                    offset = criteria.Offset;
                    limit = criteria.Limit > 0 ? criteria.Limit : limit;
                }

                if (!ordered)
                {
                    query = SortByKey(query, GetKeyInfo());
                }

                // page index
                query = query.Skip(limit * offset);

                // number of records per page
                query = query.Take(limit);
            }

            if (criteria != null)
            {
                // navigation
                if (criteria is IPagingCriteriaExpression<T>)
                {
                    var clauseExpr = criteria as IPagingCriteriaExpression<T>;
                    // navigation by expression
                    if (clauseExpr.NavigationExpr.AnySafe())
                    {
                        throw new NotSupportedException("Relationship loading via navigation not available for mongodb. Do data structure analysis or implement your custom repository.");
                    }
                }

                // navigation by string
                if (criteria.Navigation.AnySafe())
                {
                    throw new NotSupportedException("Relationship loading via navigation not available for mongodb. Do data structure analysis or implement your custom repository.");
                }
            }

            return query;
        }

        #endregion

        #region [ Supports ]

        protected FilterDefinition<T> GetKeyFilter(T entity)
        {
            var key = GetKeyInfo();
            return Builders<T>.Filter.Eq(key.Name, entity.EntityKey);
        }

        PropertyInfo _keyInfo;
        /// <summary>
        /// 
        /// </summary>
        protected PropertyInfo GetKeyInfo()
        {
            if (_keyInfo == null)
            {
                _keyInfo = Array.Find(typeof(T).GetTypeInfo()
                                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                    , x => x.GetCustomAttribute<BsonIdAttribute>() != null);

                if (_keyInfo == null)
                {
                    if (typeof(T).InheritsOrImplements(typeof(EntityBase<>))
                        || typeof(T).InheritsOrImplements(typeof(EntityBaseLog<,>)))
                    {
                        _keyInfo = Array.Find(typeof(T).GetTypeInfo()
                                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                            , x => x.Name == "Id");
                    }

                    if (_keyInfo == null)
                    {
                        _keyInfo = Array.Find(typeof(T).GetTypeInfo()
                                            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                            , x => x.Name == BsonClassMap.LookupClassMap(typeof(T)).IdMemberMap.MemberName);
                    }

                    if (_keyInfo == null)
                        throw new InvalidOperationException("Key property not found.");
                }
            }

            return _keyInfo;
        }
        /// <summary>
        /// 
        /// </summary>
        protected IQueryable<T> GetDynamicFilter<TValue>(IQueryable<T> query, PropertyInfo key, TValue value)
        {
            var entityParameter = Expression.Parameter(typeof(T), "e");

            var lambda =
                Expression.Lambda<Func<T, bool>>(
                    Expression.Equal(
                        Expression.Property(entityParameter, key),
                        value != null && value.GetType() == key.PropertyType || typeof(TValue) == key.PropertyType
                            ? Expression.Constant(value)
                                : Expression.Convert(Expression.Constant(value), key.PropertyType)),
                        entityParameter);

            return query.Where(lambda);
        }
        /// <summary>
        /// 
        /// </summary>
        protected IQueryable<T> SortByKey(IQueryable<T> query, PropertyInfo key)
        {
            try
            {
                Type t = typeof(T);
                var param = Expression.Parameter(t);

                return query.Provider.CreateQuery<T>(
                    Expression.Call(
                        typeof(Queryable),
                        "OrderBy",
                        new Type[] { t, key.PropertyType },
                        query.Expression,
                        Expression.Quote(
                            Expression.Lambda(
                                Expression.Property(param, key),
                                param))
                    ));
            }
            catch (Exception) // Probably invalid input, you can catch specifics if you want
            {
                return query; // Return unsorted query
            }
        }

        #endregion
    }
}
