//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;

namespace Mvp24Hours.Infrastructure.FileStorage.Results;

/// <summary>
/// Implementation of <see cref="IChunkedUploadStatus"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ChunkedUploadStatus"/> class.
/// </remarks>
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
public class ChunkedUploadStatus(
    string uploadId,
    string filePath,
    long totalSize,
    long chunkSize,
    int totalChunks,
    int uploadedChunks,
    long bytesUploaded,
    DateTimeOffset initiatedAt,
    DateTimeOffset? expiresAt = null,
    bool isComplete = false) : IChunkedUploadStatus
{

    /// <inheritdoc/>
    public string UploadId { get; } = uploadId ?? throw new ArgumentNullException(nameof(uploadId));

    /// <inheritdoc/>
    public string FilePath { get; } = filePath ?? throw new ArgumentNullException(nameof(filePath));

    /// <inheritdoc/>
    public long TotalSize { get; } = totalSize;

    /// <inheritdoc/>
    public long ChunkSize { get; } = chunkSize;

    /// <inheritdoc/>
    public int TotalChunks { get; } = totalChunks;

    /// <inheritdoc/>
    public int UploadedChunks { get; } = uploadedChunks;

    /// <inheritdoc/>
    public long BytesUploaded { get; } = bytesUploaded;

    /// <inheritdoc/>
    public DateTimeOffset InitiatedAt { get; } = initiatedAt;

    /// <inheritdoc/>
    public DateTimeOffset? ExpiresAt { get; } = expiresAt;

    /// <inheritdoc/>
    public bool IsComplete { get; } = isComplete;

    /// <inheritdoc/>
    public double ProgressPercentage => TotalSize > 0 ? (BytesUploaded * 100.0 / TotalSize) : 0;
}

