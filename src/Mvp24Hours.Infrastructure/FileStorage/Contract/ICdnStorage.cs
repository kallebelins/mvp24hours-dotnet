//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Interface for CDN (Content Delivery Network) integration with file storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// CDN integration provides:
    /// - Fast global content delivery
    /// - Cache invalidation
    /// - URL generation for CDN endpoints
    /// - Cache control headers
    /// </para>
    /// <para>
    /// Files stored in the underlying storage are automatically distributed to CDN edge locations
    /// for faster access by end users worldwide.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Upload a file
    /// await fileStorage.UploadAsync("images/photo.jpg", imageBytes, "image/jpeg");
    /// 
    /// // Get CDN URL for the file
    /// var cdnUrl = await cdnStorage.GetCdnUrlAsync("images/photo.jpg");
    /// // Returns: https://cdn.example.com/images/photo.jpg
    /// 
    /// // Invalidate CDN cache for a file
    /// await cdnStorage.InvalidateCacheAsync("images/photo.jpg");
    /// 
    /// // Invalidate cache for multiple files
    /// await cdnStorage.InvalidateCacheAsync(new[] { "images/photo1.jpg", "images/photo2.jpg" });
    /// </code>
    /// </example>
    public interface ICdnStorage
    {
        /// <summary>
        /// Gets the CDN URL for a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="useHttps">Whether to use HTTPS for the CDN URL.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The CDN URL for the file, or null if CDN is not configured or the file doesn't exist.</returns>
        /// <remarks>
        /// <para>
        /// The CDN URL points to the CDN endpoint instead of the origin storage. This allows
        /// clients to download files from the nearest CDN edge location for better performance.
        /// </para>
        /// <para>
        /// The CDN URL format depends on the CDN provider (e.g., CloudFront, Azure CDN, Cloudflare).
        /// </para>
        /// </remarks>
        Task<string?> GetCdnUrlAsync(
            string filePath,
            bool useHttps = true,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the CDN cache for a file, forcing the CDN to fetch a fresh copy from origin.
        /// </summary>
        /// <param name="filePath">The path of the file to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the cache was invalidated; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// Cache invalidation is useful when a file has been updated and you want to ensure
        /// users get the latest version immediately, rather than waiting for the cache to expire.
        /// </para>
        /// <para>
        /// Invalidation may take a few minutes to propagate across all CDN edge locations.
        /// </para>
        /// </remarks>
        Task<bool> InvalidateCacheAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the CDN cache for multiple files.
        /// </summary>
        /// <param name="filePaths">The paths of the files to invalidate.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of files successfully invalidated.</returns>
        /// <remarks>
        /// Batch invalidation is more efficient than invalidating files individually.
        /// </remarks>
        Task<int> InvalidateCacheAsync(
            IEnumerable<string> filePaths,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates the CDN cache for all files matching a path pattern (wildcard).
        /// </summary>
        /// <param name="pathPattern">The path pattern (e.g., "images/*" or "documents/2024/*").</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of files invalidated.</returns>
        /// <remarks>
        /// <para>
        /// Wildcard patterns allow invalidating multiple files at once. Common patterns:
        /// - "images/*" - all files in the images directory
        /// - "documents/2024/*" - all files in documents/2024 directory
        /// - "*.jpg" - all JPEG files
        /// </para>
        /// <para>
        /// Pattern syntax depends on the CDN provider.
        /// </para>
        /// </remarks>
        Task<int> InvalidateCacheByPatternAsync(
            string pathPattern,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets cache control headers for a file in the CDN.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="cacheControl">The cache control header value (e.g., "public, max-age=3600").</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the cache control was set; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// Cache control headers determine how long the CDN should cache the file before
        /// checking for updates from the origin.
        /// </para>
        /// <para>
        /// Common cache control values:
        /// - "public, max-age=3600" - cache for 1 hour
        /// - "public, max-age=86400" - cache for 24 hours
        /// - "no-cache" - always check origin
        /// </para>
        /// </remarks>
        Task<bool> SetCacheControlAsync(
            string filePath,
            string cacheControl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets cache control headers for a file.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The cache control header value, or null if not set.</returns>
        Task<string?> GetCacheControlAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Purges the entire CDN cache (use with caution).
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the cache was purged; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// This operation invalidates all cached content in the CDN. It should be used sparingly
        /// as it can impact performance and may have rate limits or costs associated with it.
        /// </para>
        /// <para>
        /// Prefer using specific invalidation methods (<see cref="InvalidateCacheAsync"/>) when possible.
        /// </para>
        /// </remarks>
        Task<bool> PurgeAllCacheAsync(
            CancellationToken cancellationToken = default);
    }
}

