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
    /// Implementation of <see cref="ISoftDeletedFile"/>.
    /// </summary>
    public class SoftDeletedFile : ISoftDeletedFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SoftDeletedFile"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="originalMetadata">The original file metadata.</param>
        /// <param name="deletedAt">When the file was deleted.</param>
        /// <param name="deletionReason">Optional reason for deletion.</param>
        /// <param name="deletedBy">Optional identifier of who deleted the file.</param>
        public SoftDeletedFile(
            string filePath,
            IFileMetadata originalMetadata,
            DateTimeOffset deletedAt,
            string? deletionReason = null,
            string? deletedBy = null)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            OriginalMetadata = originalMetadata ?? throw new ArgumentNullException(nameof(originalMetadata));
            DeletedAt = deletedAt;
            DeletionReason = deletionReason;
            DeletedBy = deletedBy;
        }

        /// <inheritdoc/>
        public string FilePath { get; }

        /// <inheritdoc/>
        public IFileMetadata OriginalMetadata { get; }

        /// <inheritdoc/>
        public DateTimeOffset DeletedAt { get; }

        /// <inheritdoc/>
        public string? DeletionReason { get; }

        /// <inheritdoc/>
        public string? DeletedBy { get; }
    }
}

