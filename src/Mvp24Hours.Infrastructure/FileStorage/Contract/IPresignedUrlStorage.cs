//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Interface for generating presigned URLs for temporary file access.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Presigned URLs allow temporary, direct access to files without requiring authentication
    /// through the application. This is useful for:
    /// - Direct browser uploads/downloads
    /// - Sharing files with external systems
    /// - Offloading file serving to cloud storage providers
    /// </para>
    /// <para>
    /// Presigned URLs are time-limited and can be configured with specific permissions (read, write).
    /// They are commonly used with cloud storage providers like AWS S3 and Azure Blob Storage.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Generate a presigned URL for download (valid for 1 hour)
    /// var downloadUrl = await presignedStorage.GenerateDownloadUrlAsync(
    ///     "documents/file.pdf",
    ///     TimeSpan.FromHours(1),
    ///     cancellationToken);
    /// 
    /// // Generate a presigned URL for upload (valid for 30 minutes)
    /// var uploadUrl = await presignedStorage.GenerateUploadUrlAsync(
    ///     "documents/new-file.pdf",
    ///     "application/pdf",
    ///     TimeSpan.FromMinutes(30),
    ///     cancellationToken);
    /// </code>
    /// </example>
    public interface IPresignedUrlStorage
    {
        /// <summary>
        /// Generates a presigned URL for downloading a file.
        /// </summary>
        /// <param name="filePath">The path of the file to download.</param>
        /// <param name="expiration">How long the URL should be valid.</param>
        /// <param name="contentType">Optional content type override for the download.</param>
        /// <param name="contentDisposition">Optional content disposition header (e.g., "attachment; filename=file.pdf").</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The presigned URL, or null if the file doesn't exist or URL generation failed.</returns>
        /// <remarks>
        /// <para>
        /// The generated URL allows direct access to the file without authentication. The URL
        /// expires after the specified duration. Once expired, the URL will no longer work.
        /// </para>
        /// <para>
        /// The URL typically includes query parameters for authentication and expiration.
        /// The exact format depends on the storage provider.
        /// </para>
        /// </remarks>
        Task<string?> GenerateDownloadUrlAsync(
            string filePath,
            TimeSpan expiration,
            string? contentType = null,
            string? contentDisposition = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a presigned URL for uploading a file.
        /// </summary>
        /// <param name="filePath">The path where the file should be uploaded.</param>
        /// <param name="contentType">The MIME type of the file to be uploaded.</param>
        /// <param name="expiration">How long the URL should be valid.</param>
        /// <param name="maxFileSize">Optional maximum file size in bytes.</param>
        /// <param name="metadata">Optional metadata to associate with the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The presigned URL for upload, or null if URL generation failed.</returns>
        /// <remarks>
        /// <para>
        /// The generated URL allows direct upload to the storage provider without going through
        /// the application server. This is useful for large file uploads or browser-based uploads.
        /// </para>
        /// <para>
        /// The client can use this URL with HTTP PUT or POST to upload the file directly.
        /// The URL expires after the specified duration.
        /// </para>
        /// </remarks>
        Task<string?> GenerateUploadUrlAsync(
            string filePath,
            string contentType,
            TimeSpan expiration,
            long? maxFileSize = null,
            System.Collections.Generic.IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a presigned URL for both upload and download (multipart upload).
        /// </summary>
        /// <param name="filePath">The path where the file should be uploaded.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="expiration">How long the URL should be valid.</param>
        /// <param name="partSize">The size of each part in bytes (for multipart uploads).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Information about the multipart upload including presigned URLs for each part.</returns>
        /// <remarks>
        /// <para>
        /// This method is useful for large file uploads that need to be split into multiple parts.
        /// Each part gets its own presigned URL for upload.
        /// </para>
        /// <para>
        /// After all parts are uploaded, a separate operation is typically required to complete
        /// the multipart upload (provider-dependent).
        /// </para>
        /// </remarks>
        Task<IMultipartUploadInfo?> GenerateMultipartUploadUrlsAsync(
            string filePath,
            string contentType,
            TimeSpan expiration,
            long partSize,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Information about a multipart upload operation.
    /// </summary>
    public interface IMultipartUploadInfo
    {
        /// <summary>
        /// Gets the upload ID for this multipart upload.
        /// </summary>
        string UploadId { get; }

        /// <summary>
        /// Gets the presigned URLs for each part of the upload.
        /// </summary>
        /// <remarks>
        /// The key is the part number (1-based), and the value is the presigned URL for that part.
        /// </remarks>
        System.Collections.Generic.IDictionary<int, string> PartUrls { get; }

        /// <summary>
        /// Gets the size of each part in bytes.
        /// </summary>
        long PartSize { get; }

        /// <summary>
        /// Gets the total number of parts.
        /// </summary>
        int TotalParts { get; }
    }
}

