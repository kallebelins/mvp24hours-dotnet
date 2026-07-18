//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;

namespace Mvp24Hours.Infrastructure.FileStorage.Results;

/// <summary>
/// Implementation of <see cref="IMultipartUploadInfo"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MultipartUploadInfo"/> class.
/// </remarks>
/// <param name="uploadId">The upload ID.</param>
/// <param name="partUrls">The presigned URLs for each part.</param>
/// <param name="partSize">The size of each part in bytes.</param>
/// <param name="totalParts">The total number of parts.</param>
public class MultipartUploadInfo(
    string uploadId,
    IDictionary<int, string> partUrls,
    long partSize,
    int totalParts) : IMultipartUploadInfo
{

    /// <inheritdoc/>
    public string UploadId { get; } = uploadId;

    /// <inheritdoc/>
    public IDictionary<int, string> PartUrls { get; } = partUrls ?? new Dictionary<int, string>();

    /// <inheritdoc/>
    public long PartSize { get; } = partSize;

    /// <inheritdoc/>
    public int TotalParts { get; } = totalParts;
}

