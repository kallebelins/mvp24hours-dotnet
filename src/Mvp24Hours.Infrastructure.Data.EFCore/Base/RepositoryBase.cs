//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    ///  <see cref="Mvp24Hours.Core.Contract.Data.IRepository"/>
    /// </summary>
    public abstract class RepositoryBase<T>
        where T : class, IEntityBase
    {
        #region [ Ctor ]

        protected RepositoryBase(DbContext _dbContext, IOptions<EFCoreRepositoryOptions> options)
        {
            this.dbContext = _dbContext ?? throw new ArgumentNullException(nameof(_dbContext));
            this.dbEntities = _dbContext.Set<T>();
            this.Options = options?.Value ?? new EFCoreRepositoryOptions();
        }

        #endregion

        #region [ Fields ]

        /// <summary>
        /// Database context
        /// </summary>
        protected readonly DbContext dbContext;
        /// <summary>
        /// Represents relationship with entities in the database
        /// </summary>
        protected DbSet<T> dbEntities;
        /// <summary>
        /// Gets the value of the user logged in the context or logged into the database
        /// </summary>
        protected abstract object EntityLogBy { get; }
        /// <summary>
        /// Repository configuration options
        /// </summary>
        protected EFCoreRepositoryOptions Options { get; private set; }

        #endregion

        #region [ Methods ]
        /// <summary>
        /// Gets database query with clause and aggregation of relationships
        /// </summary>
        protected IQueryable<T> GetQuery(IPagingCriteria criteria, bool onlyNavigation = false)
        {
            // cria query
            var query = this.dbEntities.AsQueryable();
            return GetQuery(query, criteria, onlyNavigation);
        }
        /// <summary>
        /// Gets database query with clause and aggregation of relationships
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Low complexity")]
        protected IQueryable<T> GetQuery(IQueryable<T> query, IPagingCriteria criteria, bool onlyNavigation = false)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositorybase-querycriteria-object", criteria);

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
                        foreach (var nav in clauseExpr.NavigationExpr)
                        {
                            query = query.Include(nav);
                        }
                    }
                }

                // navigation by string
                if (criteria.Navigation.AnySafe())
                {
                    foreach (var nav in criteria.Navigation)
                    {
                        query = query.Include(nav);
                    }
                }
            }

            return query;
        }
        /// <summary>
        /// Makes a code block transactional.
        /// </summary>
        protected TransactionScope CreateTransactionScope(bool isAggregate = false)
        {
            if (isAggregate || Options.TransactionIsolationLevel != null)
            {
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositorybase-transactionscope", $"isolation:{Options.TransactionIsolationLevel ?? IsolationLevel.ReadUncommitted}|timeout:{TransactionManager.MaximumTimeout}|asyncflow:enabled");
                return new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
                {
                    IsolationLevel = Options.TransactionIsolationLevel ?? IsolationLevel.ReadUncommitted,
                    Timeout = TransactionManager.MaximumTimeout
                }, TransactionScopeAsyncFlowOption.Enabled);
            }
            return null;
        }

        #endregion

        #region [ Supports ]

        PropertyInfo _keyInfo;
        /// <summary>
        /// 
        /// </summary>
        protected PropertyInfo GetKeyInfo()
        {
            if (_keyInfo == null)
            {
                _keyInfo = Array.Find(typeof(T).GetTypeInfo()
                                .GetProperties(bindingAttr: BindingFlags.Instance | BindingFlags.Public)
                                , x => x.GetCustomAttribute<KeyAttribute>() != null);

                if (_keyInfo == null)
                {
                    throw new InvalidOperationException("Key property not found.");
                }
                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-repositorybase-getkeyinfo", $"key:{_keyInfo.Name}");
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
                        ((value != null && value.GetType() == key.PropertyType) || typeof(TValue) == key.PropertyType)
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
