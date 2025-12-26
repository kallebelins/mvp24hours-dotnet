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
    /// Represents the result of a file upload operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates the outcome of a file upload, including success status, file path,
    /// metadata, and any error information. It provides convenient properties for checking success
    /// and accessing file information.
    /// </para>
    /// <para>
    /// The result is immutable and provides factory methods for creating success and failure results.
    /// </para>
    /// </remarks>
    public class FileUploadResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadResult"/> class.
        /// </summary>
        /// <param name="success">Whether the upload was successful.</param>
        /// <param name="filePath">The path where the file was stored (if successful).</param>
        /// <param name="metadata">The file metadata (if successful).</param>
        /// <param name="errorMessage">The error message (if failed).</param>
        /// <param name="exception">The exception that occurred (if failed).</param>
        /// <param name="uploadedAt">When the upload completed.</param>
        private FileUploadResult(
            bool success,
            string? filePath = null,
            IFileMetadata? metadata = null,
            string? errorMessage = null,
            Exception? exception = null,
            DateTimeOffset? uploadedAt = null)
        {
            Success = success;
            FilePath = filePath;
            Metadata = metadata;
            ErrorMessage = errorMessage;
            Exception = exception;
            UploadedAt = uploadedAt ?? DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets whether the upload was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the path where the file was stored (if successful).
        /// </summary>
        /// <remarks>
        /// This is the full path in the storage system, including any base path prefix.
        /// </remarks>
        public string? FilePath { get; }

        /// <summary>
        /// Gets the file metadata (if successful).
        /// </summary>
        /// <remarks>
        /// This includes information such as file size, content type, creation date, etc.
        /// </remarks>
        public IFileMetadata? Metadata { get; }

        /// <summary>
        /// Gets the error message if the upload failed; otherwise, <c>null</c>.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the exception that occurred during upload, if any; otherwise, <c>null</c>.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets when the upload operation completed.
        /// </summary>
        public DateTimeOffset UploadedAt { get; }

        /// <summary>
        /// Gets whether the upload failed.
        /// </summary>
        public bool IsFailure => !Success;

        /// <summary>
        /// Creates a successful upload result.
        /// </summary>
        /// <param name="filePath">The path where the file was stored.</param>
        /// <param name="metadata">The file metadata.</param>
        /// <param name="uploadedAt">When the upload completed.</param>
        /// <returns>A result indicating successful upload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
        public static FileUploadResult Successful(
            string filePath,
            IFileMetadata? metadata = null,
            DateTimeOffset? uploadedAt = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");
            }

            return new FileUploadResult(
                success: true,
                filePath: filePath,
                metadata: metadata,
                uploadedAt: uploadedAt);
        }

        /// <summary>
        /// Creates a failed upload result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the upload failed.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="uploadedAt">When the upload operation completed.</param>
        /// <returns>A result indicating failed upload.</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty.</exception>
        public static FileUploadResult Failed(
            string errorMessage,
            Exception? exception = null,
            DateTimeOffset? uploadedAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new FileUploadResult(
                success: false,
                errorMessage: errorMessage,
                exception: exception,
                uploadedAt: uploadedAt);
        }

        /// <summary>
        /// Creates a failed upload result from an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="uploadedAt">When the upload operation completed.</param>
        /// <returns>A result indicating failed upload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static FileUploadResult Failed(
            Exception exception,
            DateTimeOffset? uploadedAt = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new FileUploadResult(
                success: false,
                errorMessage: exception.Message,
                exception: exception,
                uploadedAt: uploadedAt);
        }
    }
}

