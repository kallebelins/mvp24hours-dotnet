//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using System;

namespace Mvp24Hours.Infrastructure.FileStorage.Results
{
    /// <summary>
    /// Implementation of <see cref="IChunkedUploadStatus"/>.
    /// </summary>
    public class ChunkedUploadStatus : IChunkedUploadStatus
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChunkedUploadStatus"/> class.
        /// </summary>
        /// <param name="uploadId">The upload session ID.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="totalSize">The total file size.</param>
        /// <param name="chunkSize">The chunk size.</param>
        /// <param name="totalChunks">The total number of chunks.</param>
        /// <param name="uploadedChunks">The number of uploaded chunks.</param>
        /// <param name="bytesUploaded">The total bytes uploaded.</param>
        /// <param name="initiatedAt">When the upload was initiated.</param>
        /// <param name="expiresAt">When the upload expires.</param>
        /// <param name="isComplete">Whether the upload is complete.</param>
        public ChunkedUploadStatus(
            string uploadId,
            string filePath,
            long totalSize,
            long chunkSize,
            int totalChunks,
            int uploadedChunks,
            long bytesUploaded,
            DateTimeOffset initiatedAt,
            DateTimeOffset? expiresAt = null,
            bool isComplete = false)
        {
            UploadId = uploadId ?? throw new ArgumentNullException(nameof(uploadId));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            TotalSize = totalSize;
            ChunkSize = chunkSize;
            TotalChunks = totalChunks;
            UploadedChunks = uploadedChunks;
            BytesUploaded = bytesUploaded;
            InitiatedAt = initiatedAt;
            ExpiresAt = expiresAt;
            IsComplete = isComplete;
        }

        /// <inheritdoc/>
        public string UploadId { get; }

        /// <inheritdoc/>
        public string FilePath { get; }

        /// <inheritdoc/>
        public long TotalSize { get; }

        /// <inheritdoc/>
        public long ChunkSize { get; }

        /// <inheritdoc/>
        public int TotalChunks { get; }

        /// <inheritdoc/>
        public int UploadedChunks { get; }

        /// <inheritdoc/>
        public long BytesUploaded { get; }

        /// <inheritdoc/>
        public DateTimeOffset InitiatedAt { get; }

        /// <inheritdoc/>
        public DateTimeOffset? ExpiresAt { get; }

        /// <inheritdoc/>
        public bool IsComplete { get; }

        /// <inheritdoc/>
        public double ProgressPercentage => TotalSize > 0 ? (BytesUploaded * 100.0 / TotalSize) : 0;
    }
}

