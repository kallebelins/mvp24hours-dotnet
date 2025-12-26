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
    /// Implementation of <see cref="IFileVersion"/>.
    /// </summary>
    public class FileVersion : IFileVersion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileVersion"/> class.
        /// </summary>
        /// <param name="versionId">The version identifier.</param>
        /// <param name="versionNumber">The version number.</param>
        /// <param name="metadata">The file metadata.</param>
        /// <param name="isCurrentVersion">Whether this is the current version.</param>
        /// <param name="isDeleted">Whether this version is deleted.</param>
        /// <param name="createdAt">When this version was created.</param>
        /// <param name="description">Optional description.</param>
        public FileVersion(
            string versionId,
            int versionNumber,
            IFileMetadata metadata,
            bool isCurrentVersion,
            bool isDeleted,
            DateTimeOffset createdAt,
            string? description = null)
        {
            VersionId = versionId ?? throw new ArgumentNullException(nameof(versionId));
            VersionNumber = versionNumber;
            Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            IsCurrentVersion = isCurrentVersion;
            IsDeleted = isDeleted;
            CreatedAt = createdAt;
            Description = description;
        }

        /// <inheritdoc/>
        public string VersionId { get; }

        /// <inheritdoc/>
        public int VersionNumber { get; }

        /// <inheritdoc/>
        public IFileMetadata Metadata { get; }

        /// <inheritdoc/>
        public bool IsCurrentVersion { get; }

        /// <inheritdoc/>
        public bool IsDeleted { get; }

        /// <inheritdoc/>
        public DateTimeOffset CreatedAt { get; }

        /// <inheritdoc/>
        public string? Description { get; }
    }
}

