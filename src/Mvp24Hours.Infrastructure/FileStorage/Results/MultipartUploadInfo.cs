//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.FileStorage.Results
{
    /// <summary>
    /// Implementation of <see cref="IMultipartUploadInfo"/>.
    /// </summary>
    public class MultipartUploadInfo : IMultipartUploadInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultipartUploadInfo"/> class.
        /// </summary>
        /// <param name="uploadId">The upload ID.</param>
        /// <param name="partUrls">The presigned URLs for each part.</param>
        /// <param name="partSize">The size of each part in bytes.</param>
        /// <param name="totalParts">The total number of parts.</param>
        public MultipartUploadInfo(
            string uploadId,
            IDictionary<int, string> partUrls,
            long partSize,
            int totalParts)
        {
            UploadId = uploadId;
            PartUrls = partUrls ?? new Dictionary<int, string>();
            PartSize = partSize;
            TotalParts = totalParts;
        }

        /// <inheritdoc/>
        public string UploadId { get; }

        /// <inheritdoc/>
        public IDictionary<int, string> PartUrls { get; }

        /// <inheritdoc/>
        public long PartSize { get; }

        /// <inheritdoc/>
        public int TotalParts { get; }
    }
}

