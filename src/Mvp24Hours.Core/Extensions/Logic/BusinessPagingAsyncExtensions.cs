//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.Helpers;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class BusinessPagingAsyncExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingCriteria> ToPagingCriteriaAsync(this Task<PagingCriteriaRequest> requestAsync, int? limit = null, int? offset = null, IList<string> orderBy = null, IList<string> navigation = null)
        {
            var request = await requestAsync;
            return new PagingCriteria(
                limit ?? request?.Limit ?? 0,
                offset ?? request?.Offset ?? 0,
                new ReadOnlyCollection<string>(orderBy ?? request?.OrderBy ?? []),
                new ReadOnlyCollection<string>(navigation ?? request?.Navigation ?? [])
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingCriteriaExpression<T>> ToPagingCriteriaExpressionAsync<T>(this Task<PagingCriteriaRequest> requestAsync, int? limit = null, int? offset = null)
        {
            var request = await requestAsync;
            return new PagingCriteriaExpression<T>(
                limit ?? request?.Limit ?? 0,
                offset ?? request?.Offset ?? 0,
                new ReadOnlyCollection<string>(request?.OrderBy ?? []),
                new ReadOnlyCollection<string>(request?.Navigation ?? [])
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingCriteria> NewCriteriaAsync(this Task<IPagingCriteria> criteriaAsync, int? limit = null, int? offset = null, IList<string> orderBy = null, IList<string> navigation = null)
        {
            var criteria = await criteriaAsync;
            return new PagingCriteria(
                limit ?? criteria?.Limit ?? 0,
                offset ?? criteria?.Offset ?? 0,
                new ReadOnlyCollection<string>(orderBy ?? criteria?.OrderBy?.ToList() ?? []),
                new ReadOnlyCollection<string>(navigation ?? criteria?.Navigation?.ToList() ?? [])
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingCriteriaExpression<T>> NewCriteriaExpressionAsync<T>(this Task<IPagingCriteria> criteriaAsync, int? limit = null, int? offset = null)
        {
            var criteria = await criteriaAsync;
            return new PagingCriteriaExpression<T>(
                limit ?? criteria?.Limit ?? 0,
                offset ?? criteria?.Offset ?? 0,
                new ReadOnlyCollection<string>(criteria?.OrderBy?.ToList() ?? []),
                new ReadOnlyCollection<string>(criteria?.Navigation?.ToList() ?? [])
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<T>> ToBusinessPagingAsync<T>(this Task<T> valueAsync, IPageResult page, ISummaryResult summary, IList<IMessageResult> messageResult = null, string tokenDefault = null)
        {
            T value = default;
            if (valueAsync != null)
            {
                value = await valueAsync;
            }
            return new PagingResult<T>(
                page,
                summary,
                value,
                messages: new ReadOnlyCollection<IMessageResult>(messageResult ?? new List<IMessageResult>()),
                token: tokenDefault
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<T>> ToBusinessPagingAsync<T>(this Task<T> valueAsync, IList<IMessageResult> messageResult, string tokenDefault = null)
        {
            T value = default;
            if (valueAsync != null)
            {
                value = await valueAsync;
            }
            return new PagingResult<T>(
                new PageResult(0, 0, 0),
                new SummaryResult(0, 0),
                data: value,
                messages: new ReadOnlyCollection<IMessageResult>(messageResult ?? new List<IMessageResult>()),
                token: tokenDefault
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<T>> ToBusinessPagingAsync<T>(this Task<T> valueAsync, IMessageResult messageResult, string tokenDefault = null)
        {
            T value = default;
            if (valueAsync != null)
            {
                value = await valueAsync;
            }
            return new PagingResult<T>(
                new PageResult(0, 0, 0),
                new SummaryResult(0, 0),
                data: value,
                messages: new ReadOnlyCollection<IMessageResult>(new List<IMessageResult>() { messageResult }),
                token: tokenDefault
            );
        }


        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<T>> ToBusinessPagingAsync<T>(this Task<IList<IMessageResult>> messageResultAsync, string tokenDefault = null)
        {
            var messageResult = await messageResultAsync;
            return new PagingResult<T>(
                new PageResult(0, 0, 0),
                new SummaryResult(0, 0),
                data: default,
                messages: new ReadOnlyCollection<IMessageResult>(messageResult ?? new List<IMessageResult>()),
                token: tokenDefault
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<T>> ToBusinessPagingAsync<T>(this Task<IMessageResult> messageResultAsync, string tokenDefault = null)
        {
            var messageResult = await messageResultAsync;
            return new PagingResult<T>(
                new PageResult(0, 0, 0),
                new SummaryResult(0, 0),
                data: default,
                messages: new ReadOnlyCollection<IMessageResult>(new List<IMessageResult>() { messageResult }),
                token: tokenDefault
            );
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<IList<TEntity>>> ToBusinessPagingAsync<TEntity>(this IRepositoryAsync<TEntity> repository, Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria = null, int? maxQtyByQueryDefault = null)
            where TEntity : class, IEntityBase
        {
            int limit = maxQtyByQueryDefault ?? ContantsHelper.Data.MaxQtyByQueryPage;
            int offset = 0;

            if (criteria != null)
            {
                limit = criteria.Limit > 0 ? criteria.Limit : limit;
                offset = criteria.Offset;
            }

            var totalCount = await repository.GetByCountAsync(clause);
            var totalPages = (int)Math.Ceiling((double)totalCount / limit);

            var items = await repository.GetByAsync(clause, criteria);

            var result = items.ToBusinessPaging(
                new PageResult(limit, offset, items.Count),
                new SummaryResult(totalCount, totalPages)
            );

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task<IPagingResult<IList<TEntity>>> ToBusinessPagingAsync<TEntity>(this IRepositoryAsync<TEntity> repository, IPagingCriteria criteria = null, int? maxQtyByQueryDefault = null)
            where TEntity : class, IEntityBase
        {
            int limit = maxQtyByQueryDefault ?? ContantsHelper.Data.MaxQtyByQueryPage;
            int offset = 0;

            if (criteria != null)
            {
                limit = criteria.Limit > 0 ? criteria.Limit : limit;
                offset = criteria.Offset;
            }

            var totalCount = await repository.ListCountAsync();
            var totalPages = (int)Math.Ceiling((double)totalCount / limit);

            var items = await repository.ListAsync(criteria);

            var result = items.ToBusinessPaging(
                new PageResult(limit, offset, items.Count),
                new SummaryResult(totalCount, totalPages)
            );

            return result;
        }
    }
}
