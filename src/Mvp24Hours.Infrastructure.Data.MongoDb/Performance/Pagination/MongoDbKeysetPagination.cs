//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Pagination
{
    /// <summary>
    /// Provides cursor-based (keyset) pagination for MongoDB collections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keyset pagination is more efficient than offset-based pagination for large datasets
    /// because it uses index seeks rather than full scans. It also provides consistent
    /// results when data changes between page requests.
    /// </para>
    /// <para>
    /// Requirements:
    /// <list type="bullet">
    ///   <item>The cursor field should have an index</item>
    ///   <item>The cursor field values should be unique (or combined with another field)</item>
    ///   <item>Results must be sorted by the cursor field</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Forward pagination
    /// var paginator = MongoDbKeysetPagination&lt;Order&gt;.Create(collection)
    ///     .OrderByDescending(o => o.CreatedAt)
    ///     .ThenBy(o => o.Id);
    /// 
    /// var firstPage = await paginator.GetPageAsync(pageSize: 20);
    /// var nextPage = await paginator.GetNextPageAsync(firstPage.LastCursor, pageSize: 20);
    /// 
    /// // Backward pagination
    /// var prevPage = await paginator.GetPreviousPageAsync(firstPage.FirstCursor, pageSize: 20);
    /// </code>
    /// </example>
    public class MongoDbKeysetPagination<T>
    {
        private readonly IMongoCollection<T> _collection;
        private FilterDefinition<T> _filter = Builders<T>.Filter.Empty;
        private SortDefinition<T> _sort;
        private readonly List<(string FieldName, bool Descending)> _sortFields = new();
        private ProjectionDefinition<T> _projection;

        private MongoDbKeysetPagination(IMongoCollection<T> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        /// <summary>
        /// Creates a new keyset pagination builder.
        /// </summary>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>A new pagination builder.</returns>
        public static MongoDbKeysetPagination<T> Create(IMongoCollection<T> collection)
        {
            return new MongoDbKeysetPagination<T>(collection);
        }

        /// <summary>
        /// Adds a filter to the pagination query.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> Where(Expression<Func<T, bool>> filter)
        {
            _filter = Builders<T>.Filter.Where(filter);
            return this;
        }

        /// <summary>
        /// Adds a filter definition to the pagination query.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> Where(FilterDefinition<T> filter)
        {
            _filter = filter;
            return this;
        }

        /// <summary>
        /// Adds ascending sort by field (primary sort key for cursor).
        /// </summary>
        /// <param name="field">The field expression.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> OrderBy(Expression<Func<T, object>> field)
        {
            var fieldName = GetFieldName(field);
            _sortFields.Add((fieldName, false));
            UpdateSort();
            return this;
        }

        /// <summary>
        /// Adds descending sort by field (primary sort key for cursor).
        /// </summary>
        /// <param name="field">The field expression.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> OrderByDescending(Expression<Func<T, object>> field)
        {
            var fieldName = GetFieldName(field);
            _sortFields.Add((fieldName, true));
            UpdateSort();
            return this;
        }

        /// <summary>
        /// Adds a secondary ascending sort field.
        /// </summary>
        /// <param name="field">The field expression.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> ThenBy(Expression<Func<T, object>> field)
        {
            return OrderBy(field);
        }

        /// <summary>
        /// Adds a secondary descending sort field.
        /// </summary>
        /// <param name="field">The field expression.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> ThenByDescending(Expression<Func<T, object>> field)
        {
            return OrderByDescending(field);
        }

        /// <summary>
        /// Adds a projection to limit returned fields.
        /// </summary>
        /// <param name="projection">The projection definition.</param>
        /// <returns>The builder for chaining.</returns>
        public MongoDbKeysetPagination<T> Project(ProjectionDefinition<T> projection)
        {
            _projection = projection;
            return this;
        }

        /// <summary>
        /// Gets the first page of results.
        /// </summary>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The paged result.</returns>
        public async Task<KeysetPagedResult<T>> GetPageAsync(
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var options = BuildFindOptions(pageSize);
            var cursor = await _collection.FindAsync(_filter, options, cancellationToken);
            var items = await cursor.ToListAsync(cancellationToken);

            return CreatePagedResult(items, pageSize);
        }

        /// <summary>
        /// Gets the next page of results after the specified cursor.
        /// </summary>
        /// <param name="afterCursor">The cursor values from the previous page's last item.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The paged result.</returns>
        public async Task<KeysetPagedResult<T>> GetNextPageAsync(
            Dictionary<string, BsonValue> afterCursor,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var cursorFilter = BuildAfterCursorFilter(afterCursor);
            var combinedFilter = Builders<T>.Filter.And(_filter, cursorFilter);

            var options = BuildFindOptions(pageSize);
            var cursor = await _collection.FindAsync(combinedFilter, options, cancellationToken);
            var items = await cursor.ToListAsync(cancellationToken);

            return CreatePagedResult(items, pageSize);
        }

        /// <summary>
        /// Gets the previous page of results before the specified cursor.
        /// </summary>
        /// <param name="beforeCursor">The cursor values from the next page's first item.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The paged result.</returns>
        public async Task<KeysetPagedResult<T>> GetPreviousPageAsync(
            Dictionary<string, BsonValue> beforeCursor,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var cursorFilter = BuildBeforeCursorFilter(beforeCursor);
            var combinedFilter = Builders<T>.Filter.And(_filter, cursorFilter);

            // Reverse sort for backward pagination
            var reversedSort = BuildReversedSort();

            var options = new FindOptions<T>
            {
                Sort = reversedSort,
                Limit = pageSize + 1,
                Projection = _projection
            };

            var cursor = await _collection.FindAsync(combinedFilter, options, cancellationToken);
            var items = await cursor.ToListAsync(cancellationToken);

            // Reverse items back to correct order
            items.Reverse();

            return CreatePagedResult(items, pageSize, isReversed: true);
        }

        #region Private Methods

        private void UpdateSort()
        {
            if (_sortFields.Count == 0)
            {
                _sort = null;
                return;
            }

            var sortBuilder = Builders<T>.Sort;
            SortDefinition<T> sort = null;

            foreach (var (fieldName, descending) in _sortFields)
            {
                var fieldSort = descending
                    ? sortBuilder.Descending(fieldName)
                    : sortBuilder.Ascending(fieldName);

                sort = sort == null ? fieldSort : sortBuilder.Combine(sort, fieldSort);
            }

            _sort = sort;
        }

        private SortDefinition<T> BuildReversedSort()
        {
            var sortBuilder = Builders<T>.Sort;
            SortDefinition<T> sort = null;

            foreach (var (fieldName, descending) in _sortFields)
            {
                // Reverse the sort direction
                var fieldSort = descending
                    ? sortBuilder.Ascending(fieldName)
                    : sortBuilder.Descending(fieldName);

                sort = sort == null ? fieldSort : sortBuilder.Combine(sort, fieldSort);
            }

            return sort;
        }

        private FindOptions<T> BuildFindOptions(int pageSize)
        {
            return new FindOptions<T>
            {
                Sort = _sort,
                Limit = pageSize + 1, // Get one extra to check if there are more results
                Projection = _projection
            };
        }

        private FilterDefinition<T> BuildAfterCursorFilter(Dictionary<string, BsonValue> cursor)
        {
            // Build compound filter for cursor-based pagination
            // For (field1: desc, field2: asc) after cursor (v1, v2):
            // (field1 < v1) OR (field1 == v1 AND field2 > v2)

            var filterBuilder = Builders<T>.Filter;
            var orFilters = new List<FilterDefinition<T>>();

            for (int i = 0; i < _sortFields.Count; i++)
            {
                var andFilters = new List<FilterDefinition<T>>();

                // Add equality filters for previous fields
                for (int j = 0; j < i; j++)
                {
                    var (fieldName, _) = _sortFields[j];
                    if (cursor.TryGetValue(fieldName, out var value))
                    {
                        andFilters.Add(filterBuilder.Eq(fieldName, value));
                    }
                }

                // Add comparison filter for current field
                var (currentField, descending) = _sortFields[i];
                if (cursor.TryGetValue(currentField, out var cursorValue))
                {
                    var comparisonFilter = descending
                        ? filterBuilder.Lt(currentField, cursorValue)
                        : filterBuilder.Gt(currentField, cursorValue);
                    andFilters.Add(comparisonFilter);
                }

                if (andFilters.Count > 0)
                {
                    orFilters.Add(filterBuilder.And(andFilters));
                }
            }

            return orFilters.Count > 0 ? filterBuilder.Or(orFilters) : filterBuilder.Empty;
        }

        private FilterDefinition<T> BuildBeforeCursorFilter(Dictionary<string, BsonValue> cursor)
        {
            // Build compound filter for backward pagination (inverse of after filter)
            var filterBuilder = Builders<T>.Filter;
            var orFilters = new List<FilterDefinition<T>>();

            for (int i = 0; i < _sortFields.Count; i++)
            {
                var andFilters = new List<FilterDefinition<T>>();

                for (int j = 0; j < i; j++)
                {
                    var (fieldName, _) = _sortFields[j];
                    if (cursor.TryGetValue(fieldName, out var value))
                    {
                        andFilters.Add(filterBuilder.Eq(fieldName, value));
                    }
                }

                var (currentField, descending) = _sortFields[i];
                if (cursor.TryGetValue(currentField, out var cursorValue))
                {
                    // Reverse comparison for backward pagination
                    var comparisonFilter = descending
                        ? filterBuilder.Gt(currentField, cursorValue)
                        : filterBuilder.Lt(currentField, cursorValue);
                    andFilters.Add(comparisonFilter);
                }

                if (andFilters.Count > 0)
                {
                    orFilters.Add(filterBuilder.And(andFilters));
                }
            }

            return orFilters.Count > 0 ? filterBuilder.Or(orFilters) : filterBuilder.Empty;
        }

        private KeysetPagedResult<T> CreatePagedResult(List<T> items, int pageSize, bool isReversed = false)
        {
            var hasMore = items.Count > pageSize;
            if (hasMore)
            {
                // Remove the extra item used for has-more check
                if (isReversed)
                {
                    items.RemoveAt(0);
                }
                else
                {
                    items.RemoveAt(items.Count - 1);
                }
            }

            Dictionary<string, BsonValue> firstCursor = null;
            Dictionary<string, BsonValue> lastCursor = null;

            if (items.Count > 0)
            {
                firstCursor = ExtractCursor(items[0]);
                lastCursor = ExtractCursor(items[^1]);
            }

            return new KeysetPagedResult<T>
            {
                Items = items,
                HasNextPage = hasMore,
                HasPreviousPage = firstCursor != null, // Simplified - actual check requires another query
                FirstCursor = firstCursor,
                LastCursor = lastCursor,
                PageSize = pageSize
            };
        }

        private Dictionary<string, BsonValue> ExtractCursor(T item)
        {
            var cursor = new Dictionary<string, BsonValue>();
            var document = item.ToBsonDocument();

            foreach (var (fieldName, _) in _sortFields)
            {
                if (document.Contains(fieldName))
                {
                    cursor[fieldName] = document[fieldName];
                }
            }

            return cursor;
        }

        private static string GetFieldName(Expression<Func<T, object>> expression)
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

    /// <summary>
    /// Represents the result of a keyset-paginated query.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    public class KeysetPagedResult<T>
    {
        /// <summary>
        /// Gets or sets the items in this page.
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Gets or sets whether there is a next page.
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Gets or sets whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Gets or sets the cursor for the first item (for backward pagination).
        /// </summary>
        public Dictionary<string, BsonValue> FirstCursor { get; set; }

        /// <summary>
        /// Gets or sets the cursor for the last item (for forward pagination).
        /// </summary>
        public Dictionary<string, BsonValue> LastCursor { get; set; }

        /// <summary>
        /// Gets or sets the page size.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets the number of items in this page.
        /// </summary>
        public int Count => Items?.Count ?? 0;
    }
}

