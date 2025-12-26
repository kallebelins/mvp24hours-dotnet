//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mvp24Hours.Infrastructure.FileStorage.Advanced
{
    /// <summary>
    /// Extension methods for advanced file storage features.
    /// </summary>
    public static class FileStorageAdvancedExtensions
    {
        /// <summary>
        /// Checks if the file storage provider supports presigned URLs.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns><c>true</c> if presigned URLs are supported; otherwise, <c>false</c>.</returns>
        public static bool SupportsPresignedUrls(this IFileStorage fileStorage)
        {
            return fileStorage is IPresignedUrlStorage;
        }

        /// <summary>
        /// Checks if the file storage provider supports file versioning.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns><c>true</c> if versioning is supported; otherwise, <c>false</c>.</returns>
        public static bool SupportsVersioning(this IFileStorage fileStorage)
        {
            return fileStorage is IFileVersioningStorage;
        }

        /// <summary>
        /// Checks if the file storage provider supports soft delete.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns><c>true</c> if soft delete is supported; otherwise, <c>false</c>.</returns>
        public static bool SupportsSoftDelete(this IFileStorage fileStorage)
        {
            return fileStorage is ISoftDeleteStorage;
        }

        /// <summary>
        /// Checks if the file storage provider supports image processing.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns><c>true</c> if image processing is supported; otherwise, <c>false</c>.</returns>
        public static bool SupportsImageProcessing(this IFileStorage fileStorage)
        {
            return fileStorage is IImageProcessingStorage;
        }

        /// <summary>
        /// Checks if the file storage provider supports chunked uploads.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns><c>true</c> if chunked uploads are supported; otherwise, <c>false</c>.</returns>
        public static bool SupportsChunkedUpload(this IFileStorage fileStorage)
        {
            return fileStorage is IChunkedUploadStorage;
        }

        /// <summary>
        /// Checks if the file storage provider supports CDN integration.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns><c>true</c> if CDN integration is supported; otherwise, <c>false</c>.</returns>
        public static bool SupportsCdn(this IFileStorage fileStorage)
        {
            return fileStorage is ICdnStorage;
        }

        /// <summary>
        /// Gets the presigned URL storage interface if supported.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns>The presigned URL storage interface, or null if not supported.</returns>
        public static IPresignedUrlStorage? AsPresignedUrlStorage(this IFileStorage fileStorage)
        {
            return fileStorage as IPresignedUrlStorage;
        }

        /// <summary>
        /// Gets the file versioning storage interface if supported.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns>The file versioning storage interface, or null if not supported.</returns>
        public static IFileVersioningStorage? AsVersioningStorage(this IFileStorage fileStorage)
        {
            return fileStorage as IFileVersioningStorage;
        }

        /// <summary>
        /// Gets the soft delete storage interface if supported.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns>The soft delete storage interface, or null if not supported.</returns>
        public static ISoftDeleteStorage? AsSoftDeleteStorage(this IFileStorage fileStorage)
        {
            return fileStorage as ISoftDeleteStorage;
        }

        /// <summary>
        /// Gets the image processing storage interface if supported.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns>The image processing storage interface, or null if not supported.</returns>
        public static IImageProcessingStorage? AsImageProcessingStorage(this IFileStorage fileStorage)
        {
            return fileStorage as IImageProcessingStorage;
        }

        /// <summary>
        /// Gets the chunked upload storage interface if supported.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns>The chunked upload storage interface, or null if not supported.</returns>
        public static IChunkedUploadStorage? AsChunkedUploadStorage(this IFileStorage fileStorage)
        {
            return fileStorage as IChunkedUploadStorage;
        }

        /// <summary>
        /// Gets the CDN storage interface if supported.
        /// </summary>
        /// <param name="fileStorage">The file storage instance.</param>
        /// <returns>The CDN storage interface, or null if not supported.</returns>
        public static ICdnStorage? AsCdnStorage(this IFileStorage fileStorage)
        {
            return fileStorage as ICdnStorage;
        }
    }
}

