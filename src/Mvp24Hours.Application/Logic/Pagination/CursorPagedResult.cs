//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;

namespace Mvp24Hours.Application.Logic.Pagination
{
    /// <summary>
    /// Represents a cursor-based (keyset) pagination result.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <remarks>
    /// <para>
    /// Cursor-based pagination provides better performance for large datasets compared to
    /// offset-based pagination because it uses indexed columns instead of OFFSET/SKIP.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // First page
    /// var page1 = CursorPagedResult&lt;Order&gt;.Create(
    ///     items: orders,
    ///     pageSize: 20,
    ///     hasMore: true,
    ///     nextCursor: "eyJpZCI6MTAwfQ==");
    /// 
    /// // Use cursor for next page
    /// var afterCursor = page1.NextCursor;
    /// var page2 = await repository.GetCursorPagedAsync(afterCursor, 20);
    /// </code>
    /// </example>
    [DataContract, Serializable]
    public class CursorPagedResult<T> : ICursorPagedResult<T>
    {
        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="CursorPagedResult{T}"/> class.
        /// </summary>
        protected CursorPagedResult()
        {
            Items = Array.Empty<T>();
        }

        /// <summary>
        /// Initializes a new instance with the specified parameters.
        /// </summary>
        /// <param name="items">The items in the current page.</param>
        /// <param name="pageSize">The requested page size.</param>
        /// <param name="nextCursor">The cursor for the next page.</param>
        /// <param name="previousCursor">The cursor for the previous page.</param>
        /// <param name="hasNextPage">Whether there are more items after this page.</param>
        /// <param name="hasPreviousPage">Whether there are items before this page.</param>
        public CursorPagedResult(
            IReadOnlyList<T> items,
            int pageSize,
            string? nextCursor = null,
            string? previousCursor = null,
            bool hasNextPage = false,
            bool hasPreviousPage = false)
        {
            Guard.Against.Null(items, nameof(items));
            Guard.Against.LessThan(pageSize, 1, nameof(pageSize));

            Items = items;
            PageSize = pageSize;
            NextCursor = nextCursor;
            PreviousCursor = previousCursor;
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
        }

        #endregion

        #region [ Properties ]

        /// <inheritdoc/>
        [DataMember]
        public IReadOnlyList<T> Items { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public int PageSize { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public int Count => Items?.Count ?? 0;

        /// <inheritdoc/>
        [DataMember]
        public string? NextCursor { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public string? PreviousCursor { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public bool HasNextPage { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public bool HasPreviousPage { get; init; }

        #endregion

        #region [ Factory Methods ]

        /// <summary>
        /// Creates a new cursor-paged result.
        /// </summary>
        public static CursorPagedResult<T> Create(
            IReadOnlyList<T> items,
            int pageSize,
            bool hasMore,
            string? nextCursor = null,
            string? previousCursor = null,
            bool hasPreviousPage = false)
        {
            return new CursorPagedResult<T>(
                items,
                pageSize,
                nextCursor,
                previousCursor,
                hasMore,
                hasPreviousPage);
        }

        /// <summary>
        /// Creates an empty cursor-paged result.
        /// </summary>
        public static CursorPagedResult<T> Empty(int pageSize = 20)
        {
            return new CursorPagedResult<T>(Array.Empty<T>(), pageSize);
        }

        #endregion

        #region [ Conversion Methods ]

        /// <summary>
        /// Maps the items to a different type.
        /// </summary>
        public CursorPagedResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            Guard.Against.Null(mapper, nameof(mapper));

            var mappedItems = new List<TResult>(Items.Count);
            foreach (var item in Items)
            {
                mappedItems.Add(mapper(item));
            }

            return new CursorPagedResult<TResult>(
                mappedItems,
                PageSize,
                NextCursor,
                PreviousCursor,
                HasNextPage,
                HasPreviousPage);
        }

        #endregion
    }

    /// <summary>
    /// Represents a strongly-typed cursor-based pagination result.
    /// </summary>
    /// <typeparam name="T">The type of items in the result.</typeparam>
    /// <typeparam name="TCursor">The type of the cursor.</typeparam>
    [DataContract, Serializable]
    public class CursorPagedResult<T, TCursor> : CursorPagedResult<T>, ICursorPagedResult<T, TCursor>
        where TCursor : struct
    {
        /// <summary>
        /// Initializes a new instance with strongly-typed cursors.
        /// </summary>
        public CursorPagedResult(
            IReadOnlyList<T> items,
            int pageSize,
            TCursor? nextCursorValue = null,
            TCursor? previousCursorValue = null,
            bool hasNextPage = false,
            bool hasPreviousPage = false)
            : base(
                items,
                pageSize,
                SerializeCursor(nextCursorValue),
                SerializeCursor(previousCursorValue),
                hasNextPage,
                hasPreviousPage)
        {
            NextCursorValue = nextCursorValue;
            PreviousCursorValue = previousCursorValue;
        }

        /// <inheritdoc/>
        [DataMember]
        public TCursor? NextCursorValue { get; init; }

        /// <inheritdoc/>
        [DataMember]
        public TCursor? PreviousCursorValue { get; init; }

        /// <summary>
        /// Creates a new strongly-typed cursor-paged result.
        /// </summary>
        public static CursorPagedResult<T, TCursor> Create(
            IReadOnlyList<T> items,
            int pageSize,
            TCursor? nextCursorValue,
            TCursor? previousCursorValue = null,
            bool hasNextPage = false,
            bool hasPreviousPage = false)
        {
            return new CursorPagedResult<T, TCursor>(
                items,
                pageSize,
                nextCursorValue,
                previousCursorValue,
                hasNextPage,
                hasPreviousPage);
        }

        private static string? SerializeCursor(TCursor? cursorValue)
        {
            if (!cursorValue.HasValue)
                return null;

            // Serialize cursor to Base64-encoded JSON for URL safety
            var json = JsonSerializer.Serialize(cursorValue.Value);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Deserializes a cursor string back to the cursor type.
        /// </summary>
        /// <param name="cursorString">The Base64-encoded cursor string.</param>
        /// <returns>The deserialized cursor value, or null if invalid.</returns>
        public static TCursor? DeserializeCursor(string? cursorString)
        {
            if (string.IsNullOrEmpty(cursorString))
                return null;

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursorString));
                return JsonSerializer.Deserialize<TCursor>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Represents a composite cursor for multi-field cursor-based pagination.
    /// </summary>
    [DataContract, Serializable]
    public class CompositeCursor : ICompositeCursor
    {
        private readonly Dictionary<string, object?> _fields;

        /// <summary>
        /// Initializes a new composite cursor.
        /// </summary>
        public CompositeCursor()
        {
            _fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a composite cursor with the specified fields.
        /// </summary>
        public CompositeCursor(IDictionary<string, object?> fields)
        {
            _fields = new Dictionary<string, object?>(fields, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, object?> Fields => _fields;

        /// <summary>
        /// Adds a field to the cursor.
        /// </summary>
        public CompositeCursor WithField(string name, object? value)
        {
            _fields[name] = value;
            return this;
        }

        /// <summary>
        /// Gets a field value from the cursor.
        /// </summary>
        public TValue? GetField<TValue>(string name)
        {
            if (_fields.TryGetValue(name, out var value) && value is TValue typedValue)
            {
                return typedValue;
            }
            return default;
        }

        /// <inheritdoc/>
        public string Serialize()
        {
            var json = JsonSerializer.Serialize(_fields);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Deserializes a cursor string back to a composite cursor.
        /// </summary>
        public static CompositeCursor? Deserialize(string? cursorString)
        {
            if (string.IsNullOrEmpty(cursorString))
                return null;

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursorString));
                var fields = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                return fields != null ? new CompositeCursor(fields) : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Creates a new composite cursor builder.
        /// </summary>
        public static CompositeCursor Create() => new();

        /// <summary>
        /// Creates a composite cursor from two values.
        /// </summary>
        public static CompositeCursor Create<T1, T2>(string field1, T1 value1, string field2, T2 value2)
        {
            return new CompositeCursor()
                .WithField(field1, value1)
                .WithField(field2, value2);
        }
    }
}

