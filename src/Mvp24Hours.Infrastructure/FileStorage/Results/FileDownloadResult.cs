//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.FileStorage.Results
{
    /// <summary>
    /// Represents the result of a file download operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates the outcome of a file download, including success status, file content,
    /// metadata, and any error information. It provides convenient properties for checking success
    /// and accessing file data.
    /// </para>
    /// <para>
    /// The result is immutable and provides factory methods for creating success and failure results.
    /// </para>
    /// <para>
    /// <strong>Memory Considerations:</strong>
    /// For large files, consider using <see cref="Contract.IFileStorage.DownloadToStreamAsync"/> or
    /// <see cref="Contract.IFileStorage.DownloadAsChunksAsync"/> to avoid loading the entire file
    /// into memory.
    /// </para>
    /// </remarks>
    public class FileDownloadResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileDownloadResult"/> class.
        /// </summary>
        /// <param name="success">Whether the download was successful.</param>
        /// <param name="content">The file content as a byte array (if successful).</param>
        /// <param name="metadata">The file metadata (if successful).</param>
        /// <param name="errorMessage">The error message (if failed).</param>
        /// <param name="exception">The exception that occurred (if failed).</param>
        /// <param name="downloadedAt">When the download completed.</param>
        private FileDownloadResult(
            bool success,
            byte[]? content = null,
            IFileMetadata? metadata = null,
            string? errorMessage = null,
            Exception? exception = null,
            DateTimeOffset? downloadedAt = null)
        {
            Success = success;
            Content = content;
            Metadata = metadata;
            ErrorMessage = errorMessage;
            Exception = exception;
            DownloadedAt = downloadedAt ?? DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets whether the download was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the file content as a byte array (if successful).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property contains the complete file content. For large files, this may consume
        /// significant memory. Consider using streaming download methods for better memory efficiency.
        /// </para>
        /// <para>
        /// This is <c>null</c> if the download failed or if the file doesn't exist.
        /// </para>
        /// </remarks>
        public byte[]? Content { get; }

        /// <summary>
        /// Gets the file metadata (if successful).
        /// </summary>
        /// <remarks>
        /// This includes information such as file size, content type, creation date, etc.
        /// </remarks>
        public IFileMetadata? Metadata { get; }

        /// <summary>
        /// Gets the error message if the download failed; otherwise, <c>null</c>.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the exception that occurred during download, if any; otherwise, <c>null</c>.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets when the download operation completed.
        /// </summary>
        public DateTimeOffset DownloadedAt { get; }

        /// <summary>
        /// Gets whether the download failed.
        /// </summary>
        public bool IsFailure => !Success;

        /// <summary>
        /// Gets whether the file was not found.
        /// </summary>
        /// <remarks>
        /// This is a convenience property that checks if the error message indicates a file not found error.
        /// </remarks>
        public bool IsNotFound => !Success && ErrorMessage != null &&
            (ErrorMessage.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
             ErrorMessage.Contains("does not exist", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Creates a successful download result.
        /// </summary>
        /// <param name="content">The file content as a byte array.</param>
        /// <param name="metadata">The file metadata.</param>
        /// <param name="downloadedAt">When the download completed.</param>
        /// <returns>A result indicating successful download.</returns>
        /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
        public static FileDownloadResult Successful(
            byte[] content,
            IFileMetadata? metadata = null,
            DateTimeOffset? downloadedAt = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            return new FileDownloadResult(
                success: true,
                content: content,
                metadata: metadata,
                downloadedAt: downloadedAt);
        }

        /// <summary>
        /// Creates a failed download result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the download failed.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="downloadedAt">When the download operation completed.</param>
        /// <returns>A result indicating failed download.</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty.</exception>
        public static FileDownloadResult Failed(
            string errorMessage,
            Exception? exception = null,
            DateTimeOffset? downloadedAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new FileDownloadResult(
                success: false,
                errorMessage: errorMessage,
                exception: exception,
                downloadedAt: downloadedAt);
        }

        /// <summary>
        /// Creates a failed download result from an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="downloadedAt">When the download operation completed.</param>
        /// <returns>A result indicating failed download.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static FileDownloadResult Failed(
            Exception exception,
            DateTimeOffset? downloadedAt = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new FileDownloadResult(
                success: false,
                errorMessage: exception.Message,
                exception: exception,
                downloadedAt: downloadedAt);
        }

        /// <summary>
        /// Creates a "file not found" download result.
        /// </summary>
        /// <param name="filePath">The path of the file that was not found.</param>
        /// <param name="downloadedAt">When the download operation completed.</param>
        /// <returns>A result indicating file not found.</returns>
        /// <exception cref="ArgumentException">Thrown when filePath is null or empty.</exception>
        public static FileDownloadResult NotFound(
            string filePath,
            DateTimeOffset? downloadedAt = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }

            return new FileDownloadResult(
                success: false,
                errorMessage: $"File not found: {filePath}",
                downloadedAt: downloadedAt);
        }
    }
}

