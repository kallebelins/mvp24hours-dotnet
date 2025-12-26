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
    /// Interface for chunked upload operations for large files.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Chunked upload allows uploading large files in smaller chunks, providing:
    /// - Better reliability (resume failed uploads)
    /// - Progress tracking
    /// - Reduced memory usage
    /// - Support for very large files
    /// </para>
    /// <para>
    /// The upload process typically involves:
    /// 1. Initiating an upload session
    /// 2. Uploading chunks sequentially or in parallel
    /// 3. Completing the upload (assembling chunks into final file)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Initiate a chunked upload
    /// var uploadId = await chunkedUpload.InitiateChunkedUploadAsync(
    ///     "documents/large-file.zip",
    ///     "application/zip",
    ///     totalSize: 100 * 1024 * 1024, // 100MB
    ///     chunkSize: 5 * 1024 * 1024,   // 5MB chunks
    ///     cancellationToken);
    /// 
    /// // Upload chunks
    /// for (int i = 0; i &lt; totalChunks; i++)
    /// {
    ///     var chunkData = GetChunk(i);
    ///     await chunkedUpload.UploadChunkAsync(
    ///         uploadId,
    ///         chunkNumber: i + 1,
    ///         chunkData,
    ///         cancellationToken);
    /// }
    /// 
    /// // Complete the upload
    /// var result = await chunkedUpload.CompleteChunkedUploadAsync(
    ///     uploadId,
    ///     cancellationToken);
    /// </code>
    /// </example>
    public interface IChunkedUploadStorage
    {
        /// <summary>
        /// Initiates a chunked upload session.
        /// </summary>
        /// <param name="filePath">The path where the file should be stored.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="totalSize">The total size of the file in bytes.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="metadata">Optional metadata to associate with the file.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload session ID, or null if initiation failed.</returns>
        /// <remarks>
        /// <para>
        /// This method creates a temporary upload session. Chunks can then be uploaded
        /// using the returned upload ID. The session expires after a provider-specific
        /// timeout period if not completed.
        /// </para>
        /// <para>
        /// The chunk size should be chosen based on network conditions and provider limits.
        /// Common chunk sizes are 5MB to 100MB.
        /// </para>
        /// </remarks>
        Task<string?> InitiateChunkedUploadAsync(
            string filePath,
            string contentType,
            long totalSize,
            long chunkSize,
            IDictionary<string, string>? metadata = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a chunk of data for a chunked upload session.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="chunkNumber">The chunk number (1-based).</param>
        /// <param name="chunkData">The chunk data.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the chunk was uploaded successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// Chunks should be uploaded in order (chunk 1, chunk 2, etc.), though some providers
        /// may support parallel chunk uploads for better performance.
        /// </para>
        /// <para>
        /// If a chunk upload fails, it can be retried without affecting other chunks.
        /// </para>
        /// </remarks>
        Task<bool> UploadChunkAsync(
            string uploadId,
            int chunkNumber,
            byte[] chunkData,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a chunk from a stream.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="chunkNumber">The chunk number (1-based).</param>
        /// <param name="chunkStream">The stream containing the chunk data.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the chunk was uploaded successfully; otherwise, <c>false</c>.</returns>
        Task<bool> UploadChunkFromStreamAsync(
            string uploadId,
            int chunkNumber,
            System.IO.Stream chunkStream,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes a chunked upload, assembling all chunks into the final file.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload result for the completed file.</returns>
        /// <remarks>
        /// <para>
        /// This method verifies that all chunks have been uploaded and assembles them into
        /// the final file. After completion, the upload session is closed and cannot be used again.
        /// </para>
        /// <para>
        /// If chunks are missing or corrupted, the completion will fail.
        /// </para>
        /// </remarks>
        Task<Results.FileUploadResult> CompleteChunkedUploadAsync(
            string uploadId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aborts a chunked upload session, discarding all uploaded chunks.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the upload was aborted; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This is useful for cleaning up failed or cancelled uploads.
        /// </remarks>
        Task<bool> AbortChunkedUploadAsync(
            string uploadId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a chunked upload session.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The upload status, or null if the session doesn't exist.</returns>
        Task<IChunkedUploadStatus?> GetUploadStatusAsync(
            string uploadId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists all chunks uploaded for a session.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A list of uploaded chunk numbers.</returns>
        /// <remarks>
        /// This is useful for resuming a failed upload by identifying which chunks
        /// still need to be uploaded.
        /// </remarks>
        Task<IList<int>> ListUploadedChunksAsync(
            string uploadId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Status of a chunked upload session.
    /// </summary>
    public interface IChunkedUploadStatus
    {
        /// <summary>
        /// Gets the upload session ID.
        /// </summary>
        string UploadId { get; }

        /// <summary>
        /// Gets the file path where the file will be stored.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// Gets the total size of the file in bytes.
        /// </summary>
        long TotalSize { get; }

        /// <summary>
        /// Gets the size of each chunk in bytes.
        /// </summary>
        long ChunkSize { get; }

        /// <summary>
        /// Gets the total number of chunks.
        /// </summary>
        int TotalChunks { get; }

        /// <summary>
        /// Gets the number of chunks that have been uploaded.
        /// </summary>
        int UploadedChunks { get; }

        /// <summary>
        /// Gets the total bytes uploaded so far.
        /// </summary>
        long BytesUploaded { get; }

        /// <summary>
        /// Gets when the upload session was initiated.
        /// </summary>
        DateTimeOffset InitiatedAt { get; }

        /// <summary>
        /// Gets when the upload session expires (if applicable).
        /// </summary>
        DateTimeOffset? ExpiresAt { get; }

        /// <summary>
        /// Gets whether the upload is complete.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Gets the upload progress as a percentage (0-100).
        /// </summary>
        double ProgressPercentage => TotalSize > 0 ? (BytesUploaded * 100.0 / TotalSize) : 0;
    }
}

