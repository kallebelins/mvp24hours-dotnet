//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.FileStorage.Contract
{
    /// <summary>
    /// Unified interface for file storage operations across different providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a consistent API for file storage operations regardless of the
    /// underlying storage provider (local filesystem, Azure Blob Storage, Amazon S3, etc.).
    /// All operations are asynchronous and support cancellation tokens for proper resource management.
    /// </para>
    /// <para>
    /// <strong>Streaming Support:</strong>
    /// The interface supports both traditional byte array uploads and streaming uploads/downloads
    /// for efficient handling of large files without loading entire files into memory.
    /// </para>
    /// <para>
    /// <strong>Path Handling:</strong>
    /// File paths are provider-agnostic strings. The implementation handles provider-specific
    /// path formatting (e.g., container/bucket names for cloud storage).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Upload a file
    /// var fileBytes = File.ReadAllBytes("document.pdf");
    /// var result = await fileStorage.UploadAsync("documents/document.pdf", fileBytes, "application/pdf", cancellationToken);
    /// if (result.Success)
    /// {
    ///     Console.WriteLine($"File uploaded: {result.FilePath}");
    /// }
    /// 
    /// // Download a file
    /// var downloadResult = await fileStorage.DownloadAsync("documents/document.pdf", cancellationToken);
    /// if (downloadResult.Success)
    /// {
    ///     await File.WriteAllBytesAsync("downloaded.pdf", downloadResult.Content, cancellationToken);
    /// }
    /// </code>
    /// </example>
    public interface IFileStorage
    {
        /// <summary>
        /// Uploads a file from a byte array.
        /// </summary>
        /// <param name="filePath">The path where the file should be stored.</param>
        /// <param name="content">The file content as a byte array.</param>
        /// <param name="contentType">The MIME type of the file (e.g., "application/pdf", "image/jpeg").</param>
        /// <param name="metadata">Optional metadata to associate with the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with file path and metadata.</returns>
        /// <remarks>
        /// <para>
        /// This method uploads the entire file content from memory. For large files, consider
        /// using <see cref="UploadFromStreamAsync"/> to avoid loading the entire file into memory.
        /// </para>
        /// <para>
        /// The file path can include directories/subdirectories. The provider will create necessary
        /// directory structures if they don't exist (where supported).
        /// </para>
        /// </remarks>
        Task<FileUploadResult> UploadAsync(
            string filePath,
            byte[] content,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file from a stream.
        /// </summary>
        /// <param name="filePath">The path where the file should be stored.</param>
        /// <param name="stream">The stream containing the file content.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="metadata">Optional metadata to associate with the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with file path and metadata.</returns>
        /// <remarks>
        /// <para>
        /// This method is recommended for large files as it streams the content without loading
        /// the entire file into memory. The stream is read from its current position to the end.
        /// </para>
        /// <para>
        /// The stream is not disposed by this method. The caller is responsible for disposing
        /// the stream after the operation completes.
        /// </para>
        /// </remarks>
        Task<FileUploadResult> UploadFromStreamAsync(
            string filePath,
            Stream stream,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a file from an async enumerable of byte chunks (for streaming uploads).
        /// </summary>
        /// <param name="filePath">The path where the file should be stored.</param>
        /// <param name="chunks">The async enumerable of byte chunks.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="metadata">Optional metadata to associate with the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with file path and metadata.</returns>
        /// <remarks>
        /// <para>
        /// This method is useful for streaming uploads where data arrives in chunks (e.g., from
        /// HTTP requests or real-time data sources). Each chunk is processed as it arrives,
        /// minimizing memory usage.
        /// </para>
        /// <para>
        /// The chunks are concatenated in order to form the complete file content.
        /// </para>
        /// </remarks>
        Task<FileUploadResult> UploadFromChunksAsync(
            string filePath,
            IAsyncEnumerable<byte[]> chunks,
            string contentType,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file as a byte array.
        /// </summary>
        /// <param name="filePath">The path of the file to download.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result containing the file content and metadata, or an error if the file doesn't exist.</returns>
        /// <remarks>
        /// <para>
        /// This method loads the entire file into memory. For large files, consider using
        /// <see cref="DownloadToStreamAsync"/> to stream the content directly.
        /// </para>
        /// <para>
        /// If the file doesn't exist, the result will indicate failure with an appropriate error message.
        /// </para>
        /// </remarks>
        Task<FileDownloadResult> DownloadAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file to a stream.
        /// </summary>
        /// <param name="filePath">The path of the file to download.</param>
        /// <param name="destinationStream">The stream to write the file content to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A result indicating success or failure with file metadata.</returns>
        /// <remarks>
        /// <para>
        /// This method is recommended for large files as it streams the content without loading
        /// the entire file into memory. The stream is written from its current position.
        /// </para>
        /// <para>
        /// The stream is not disposed by this method. The caller is responsible for disposing
        /// the stream after the operation completes.
        /// </para>
        /// </remarks>
        Task<FileDownloadResult> DownloadToStreamAsync(
            string filePath,
            Stream destinationStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file as an async enumerable of byte chunks (for streaming downloads).
        /// </summary>
        /// <param name="filePath">The path of the file to download.</param>
        /// <param name="chunkSize">The size of each chunk in bytes. Default is 64KB.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of byte chunks, or null if the file doesn't exist.</returns>
        /// <remarks>
        /// <para>
        /// This method is useful for streaming downloads where data should be processed in chunks
        /// (e.g., sending to HTTP responses or processing large files incrementally). Each chunk
        /// is yielded as it's read, minimizing memory usage.
        /// </para>
        /// <para>
        /// The chunks are yielded in order and can be concatenated to form the complete file content.
        /// </para>
        /// </remarks>
        IAsyncEnumerable<byte[]> DownloadAsChunksAsync(
            string filePath,
            int chunkSize = 65536,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a file exists at the specified path.
        /// </summary>
        /// <param name="filePath">The path of the file to check.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
        Task<bool> ExistsAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a file at the specified path.
        /// </summary>
        /// <param name="filePath">The path of the file to delete.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file was deleted; <c>false</c> if the file doesn't exist.</returns>
        /// <remarks>
        /// <para>
        /// This method performs a hard delete. If soft delete is supported by the provider,
        /// it may be implemented as a separate method or via options.
        /// </para>
        /// <para>
        /// If the file doesn't exist, the method returns <c>false</c> without throwing an exception.
        /// </para>
        /// </remarks>
        Task<bool> DeleteAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets metadata for a file at the specified path.
        /// </summary>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The file metadata, or <c>null</c> if the file doesn't exist.</returns>
        Task<IFileMetadata?> GetMetadataAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists files in a directory (or container/bucket prefix).
        /// </summary>
        /// <param name="directoryPath">The directory path or prefix to list files from.</param>
        /// <param name="recursive">Whether to list files recursively in subdirectories.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable of file metadata.</returns>
        /// <remarks>
        /// <para>
        /// The directory path can be empty or "/" to list files at the root level.
        /// For cloud storage providers, this corresponds to listing files with a specific prefix.
        /// </para>
        /// <para>
        /// The results are streamed as they're discovered, making this efficient for large directories.
        /// </para>
        /// </remarks>
        IAsyncEnumerable<IFileMetadata> ListFilesAsync(
            string directoryPath = "",
            bool recursive = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Copies a file from one path to another.
        /// </summary>
        /// <param name="sourcePath">The path of the source file.</param>
        /// <param name="destinationPath">The path where the file should be copied.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file was copied successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// This operation may be optimized by the provider (e.g., server-side copy for cloud storage)
        /// without downloading and re-uploading the file.
        /// </para>
        /// <para>
        /// If the source file doesn't exist, the method returns <c>false</c>.
        /// If the destination file already exists, it may be overwritten (provider-dependent).
        /// </para>
        /// </remarks>
        Task<bool> CopyAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves (renames) a file from one path to another.
        /// </summary>
        /// <param name="sourcePath">The path of the source file.</param>
        /// <param name="destinationPath">The path where the file should be moved.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the file was moved successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// This operation may be optimized by the provider (e.g., server-side move for cloud storage)
        /// without downloading and re-uploading the file.
        /// </para>
        /// <para>
        /// If the source file doesn't exist, the method returns <c>false</c>.
        /// If the destination file already exists, it may be overwritten (provider-dependent).
        /// </para>
        /// </remarks>
        Task<bool> MoveAsync(
            string sourcePath,
            string destinationPath,
            CancellationToken cancellationToken = default);
    }
}

