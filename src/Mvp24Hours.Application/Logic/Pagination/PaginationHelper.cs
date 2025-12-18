//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Pagination;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Mvp24Hours.Application.Logic.Pagination
{
    /// <summary>
    /// Provides helper methods for pagination operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This helper class provides utilities for:
    /// <list type="bullet">
    ///   <item><description>Pagination metadata calculation</description></item>
    ///   <item><description>Cursor encoding/decoding</description></item>
    ///   <item><description>Page number validation</description></item>
    ///   <item><description>Offset/limit conversions</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class PaginationHelper
    {
        #region [ Constants ]

        /// <summary>
        /// Default page size when not specified.
        /// </summary>
        public const int DefaultPageSize = 20;

        /// <summary>
        /// Maximum page size allowed.
        /// </summary>
        public const int MaxPageSize = 100;

        /// <summary>
        /// Minimum page size allowed.
        /// </summary>
        public const int MinPageSize = 1;

        #endregion

        #region [ Page Calculations ]

        /// <summary>
        /// Calculates the total number of pages.
        /// </summary>
        /// <param name="totalCount">The total number of items.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>The total number of pages.</returns>
        public static int CalculateTotalPages(int totalCount, int pageSize)
        {
            if (pageSize <= 0) return 0;
            return (int)Math.Ceiling((double)totalCount / pageSize);
        }

        /// <summary>
        /// Calculates the offset (skip count) for a given page.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>The number of items to skip.</returns>
        public static int CalculateOffset(int page, int pageSize)
        {
            return (Math.Max(1, page) - 1) * pageSize;
        }

        /// <summary>
        /// Converts offset/limit to page number (1-based).
        /// </summary>
        /// <param name="offset">The offset (number of items to skip).</param>
        /// <param name="limit">The limit (page size).</param>
        /// <returns>The page number (1-based).</returns>
        public static int OffsetToPage(int offset, int limit)
        {
            if (limit <= 0) return 1;
            return (offset / limit) + 1;
        }

        /// <summary>
        /// Normalizes the page number to be within valid range.
        /// </summary>
        /// <param name="page">The requested page number.</param>
        /// <param name="totalPages">The total number of pages.</param>
        /// <returns>A valid page number.</returns>
        public static int NormalizePage(int page, int totalPages)
        {
            if (totalPages <= 0) return 1;
            return Math.Max(1, Math.Min(page, totalPages));
        }

        /// <summary>
        /// Normalizes the page size to be within allowed range.
        /// </summary>
        /// <param name="pageSize">The requested page size.</param>
        /// <param name="maxPageSize">The maximum allowed page size (default: 100).</param>
        /// <returns>A valid page size.</returns>
        public static int NormalizePageSize(int pageSize, int maxPageSize = MaxPageSize)
        {
            return Math.Max(MinPageSize, Math.Min(pageSize, maxPageSize));
        }

        /// <summary>
        /// Creates pagination metadata from IPagingCriteria.
        /// </summary>
        /// <param name="criteria">The paging criteria.</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <returns>Pagination metadata.</returns>
        public static PaginationMetadata FromCriteria(IPagingCriteria criteria, int totalCount)
        {
            Guard.Against.Null(criteria, nameof(criteria));

            var pageSize = criteria.Limit > 0 ? criteria.Limit : DefaultPageSize;
            var page = OffsetToPage(criteria.Offset, pageSize);
            var totalPages = CalculateTotalPages(totalCount, pageSize);

            return new PaginationMetadata
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }

        #endregion

        #region [ Range Calculations ]

        /// <summary>
        /// Gets the start index (1-based) for the current page.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <returns>The start index (1-based), or 0 if no items.</returns>
        public static int GetStartIndex(int page, int pageSize, int totalCount)
        {
            if (totalCount == 0) return 0;
            return CalculateOffset(page, pageSize) + 1;
        }

        /// <summary>
        /// Gets the end index (1-based) for the current page.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <returns>The end index (1-based).</returns>
        public static int GetEndIndex(int page, int pageSize, int totalCount)
        {
            return Math.Min(page * pageSize, totalCount);
        }

        /// <summary>
        /// Formats a display string for showing items range.
        /// </summary>
        /// <param name="page">The page number (1-based).</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <param name="totalCount">The total number of items.</param>
        /// <param name="format">The format string (default: "Showing {0}-{1} of {2}").</param>
        /// <returns>The formatted range string.</returns>
        public static string FormatRange(int page, int pageSize, int totalCount, string format = "Showing {0}-{1} of {2}")
        {
            var start = GetStartIndex(page, pageSize, totalCount);
            var end = GetEndIndex(page, pageSize, totalCount);
            return string.Format(format, start, end, totalCount);
        }

        #endregion

        #region [ Cursor Operations ]

        /// <summary>
        /// Encodes a cursor value to a URL-safe Base64 string.
        /// </summary>
        /// <typeparam name="T">The cursor type.</typeparam>
        /// <param name="cursorValue">The cursor value.</param>
        /// <returns>The encoded cursor string.</returns>
        public static string EncodeCursor<T>(T cursorValue)
        {
            var json = JsonSerializer.Serialize(cursorValue);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }

        /// <summary>
        /// Decodes a cursor string back to the cursor value.
        /// </summary>
        /// <typeparam name="T">The cursor type.</typeparam>
        /// <param name="cursorString">The encoded cursor string.</param>
        /// <returns>The decoded cursor value, or default if invalid.</returns>
        public static T? DecodeCursor<T>(string? cursorString)
        {
            if (string.IsNullOrEmpty(cursorString))
                return default;

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursorString));
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Creates a composite cursor from multiple field values.
        /// </summary>
        /// <param name="fields">The cursor fields as key-value pairs.</param>
        /// <returns>The encoded composite cursor string.</returns>
        public static string EncodeCompositeCursor(params (string key, object? value)[] fields)
        {
            var dict = fields.ToDictionary(f => f.key, f => f.value);
            return EncodeCursor(dict);
        }

        /// <summary>
        /// Decodes a composite cursor string.
        /// </summary>
        /// <param name="cursorString">The encoded cursor string.</param>
        /// <returns>The decoded fields as a dictionary.</returns>
        public static Dictionary<string, JsonElement>? DecodeCompositeCursor(string? cursorString)
        {
            return DecodeCursor<Dictionary<string, JsonElement>>(cursorString);
        }

        #endregion

        #region [ Page Number Generation ]

        /// <summary>
        /// Generates a list of page numbers for pagination UI.
        /// </summary>
        /// <param name="currentPage">The current page number.</param>
        /// <param name="totalPages">The total number of pages.</param>
        /// <param name="windowSize">The number of pages to show around current page (default: 2).</param>
        /// <returns>A list of page numbers to display (includes -1 for ellipsis).</returns>
        /// <example>
        /// <code>
        /// // Current page 5, total 10 pages, window 2
        /// var pages = PaginationHelper.GeneratePageNumbers(5, 10, 2);
        /// // Returns: [1, -1, 3, 4, 5, 6, 7, -1, 10]
        /// // Where -1 represents ellipsis (...)
        /// </code>
        /// </example>
        public static IReadOnlyList<int> GeneratePageNumbers(int currentPage, int totalPages, int windowSize = 2)
        {
            if (totalPages <= 0)
                return Array.Empty<int>();

            var pages = new List<int>();
            var startPage = Math.Max(1, currentPage - windowSize);
            var endPage = Math.Min(totalPages, currentPage + windowSize);

            // Always show first page
            if (startPage > 1)
            {
                pages.Add(1);
                if (startPage > 2)
                    pages.Add(-1); // Ellipsis
            }

            // Add window pages
            for (int i = startPage; i <= endPage; i++)
            {
                pages.Add(i);
            }

            // Always show last page
            if (endPage < totalPages)
            {
                if (endPage < totalPages - 1)
                    pages.Add(-1); // Ellipsis
                pages.Add(totalPages);
            }

            return pages;
        }

        #endregion

        #region [ Validation ]

        /// <summary>
        /// Validates pagination parameters and throws if invalid.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="maxPageSize">The maximum allowed page size.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when parameters are invalid.</exception>
        public static void ValidateParameters(int page, int pageSize, int maxPageSize = MaxPageSize)
        {
            Guard.Against.LessThan(page, 1, nameof(page), "Page number must be at least 1.");
            Guard.Against.LessThan(pageSize, 1, nameof(pageSize), "Page size must be at least 1.");
            Guard.Against.GreaterThan(pageSize, maxPageSize, nameof(pageSize), $"Page size cannot exceed {maxPageSize}.");
        }

        /// <summary>
        /// Tries to validate pagination parameters without throwing.
        /// </summary>
        /// <param name="page">The page number.</param>
        /// <param name="pageSize">The page size.</param>
        /// <param name="maxPageSize">The maximum allowed page size.</param>
        /// <param name="errorMessage">The error message if validation fails.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public static bool TryValidateParameters(int page, int pageSize, int maxPageSize, out string? errorMessage)
        {
            errorMessage = null;

            if (page < 1)
            {
                errorMessage = "Page number must be at least 1.";
                return false;
            }

            if (pageSize < 1)
            {
                errorMessage = "Page size must be at least 1.";
                return false;
            }

            if (pageSize > maxPageSize)
            {
                errorMessage = $"Page size cannot exceed {maxPageSize}.";
                return false;
            }

            return true;
        }

        #endregion
    }

    /// <summary>
    /// Represents pagination metadata without actual items.
    /// </summary>
    public class PaginationMetadata
    {
        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages.
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets whether there is a next page.
        /// </summary>
        public bool HasNextPage { get; set; }

        /// <summary>
        /// Gets or sets whether there is a previous page.
        /// </summary>
        public bool HasPreviousPage { get; set; }

        /// <summary>
        /// Gets whether this is the first page.
        /// </summary>
        public bool IsFirstPage => CurrentPage == 1;

        /// <summary>
        /// Gets whether this is the last page.
        /// </summary>
        public bool IsLastPage => CurrentPage >= TotalPages;

        /// <summary>
        /// Gets the start index (1-based).
        /// </summary>
        public int StartIndex => TotalCount == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

        /// <summary>
        /// Gets the end index (1-based).
        /// </summary>
        public int EndIndex => Math.Min(CurrentPage * PageSize, TotalCount);
    }
}

