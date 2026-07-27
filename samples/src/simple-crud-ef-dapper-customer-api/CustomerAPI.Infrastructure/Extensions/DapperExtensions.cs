using Dapper;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Infrastructure.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class DapperExtensions
    {
        private const int DefaultPageLimit = 300;

        /// <summary>
        /// Perform query using dapper with pagination
        /// </summary>
        public static Task<IPagingResult<IEnumerable<T>>> QueryPagingResultAsync<T>(
            this IDbConnection connection,
            PagingCriteriaRequest pagingCriteria = null,
            string whereSql = null,
            dynamic whereParams = null,
            string orderBySql = "id asc",
            CancellationToken cancellationToken = default)
            where T : class
        {
            return QueryPagingResultAsync<T>(
                connection,
                pagingCriteria.ToPagingCriteria(),
                whereSql,
                whereParams,
                orderBySql,
                cancellationToken);
        }

        /// <summary>
        /// Perform query using dapper with pagination
        /// </summary>
        public static Task<IPagingResult<IEnumerable<T>>> QueryPagingResultAsync<T>(
            this IDbConnection connection,
            IPagingCriteria pagingCriteria = null,
            string whereSql = null,
            dynamic whereParams = null,
            string orderBySql = "id asc",
            CancellationToken cancellationToken = default)
            where T : class
        {
            int limit = DefaultPageLimit;
            int offset = 0;

            // set pagination pattern
            if (pagingCriteria != null)
            {
                limit = pagingCriteria.Limit > 0 ? pagingCriteria.Limit : limit;
                offset = pagingCriteria.Offset;
            }

            return QueryPagingResultAsync<T>(
                connection,
                limit,
                offset,
                whereSql,
                whereParams,
                orderBySql,
                cancellationToken);
        }

        /// <summary>
        /// Perform query using dapper with pagination
        /// </summary>
        public static async Task<IPagingResult<IEnumerable<T>>> QueryPagingResultAsync<T>(
            this IDbConnection connection,
            int limit,
            int offset,
            string whereSql = null,
            dynamic whereParams = null,
            string orderBySql = "id asc",
            CancellationToken cancellationToken = default)
            where T : class
        {
            // get number of rows with filter
            var sqlCountBuilder = new SqlBuilder();
            var sqlCount = sqlCountBuilder.AddTemplate($"select count(0) from {typeof(T).Name} /**where**/");

            // get list with pagination
            var sqlQueryBuilder = new SqlBuilder();

#pragma warning disable S125 // Sections of code should not be commented out
            /* MySql / PostgreSql */
            // var sqlQuery = sqlQueryBuilder.AddTemplate($"select * from {typeof(T).Name} /**where**/ /**orderby**/ limit @limit offset @offset");

            /* SqlServer */
            var sqlQuery = sqlQueryBuilder.AddTemplate($"select * from {typeof(T).Name} /**where**/ /**orderby**/ offset @offset rows fetch next @limit rows only");
#pragma warning restore S125 // Sections of code should not be commented out

            sqlQueryBuilder.AddParameters(new { limit, offset });

            // add filter
            if (whereSql.HasValue())
            {
                sqlCountBuilder.Where(whereSql, whereParams);
                sqlQueryBuilder.Where(whereSql, whereParams);
            }

            // add ordering
            sqlQueryBuilder.OrderBy(orderBySql);

            // get total lines and pages
            var countCommand = new CommandDefinition(
                sqlCount.RawSql,
                sqlCount.Parameters,
                cancellationToken: cancellationToken);
            int totalCount = await connection.QuerySingleAsync<int>(countCommand);
            var totalPages = (int)Math.Ceiling((double)totalCount / limit);

            // get paginated records
            var queryCommand = new CommandDefinition(
                sqlQuery.RawSql,
                sqlQuery.Parameters,
                cancellationToken: cancellationToken);
            var result = await connection.QueryAsync<T>(queryCommand);

            return result.ToBusinessPaging(
                new PageResult(limit, offset, result.Count()),
                new SummaryResult(totalCount, totalPages)
            );
        }
    }
}
