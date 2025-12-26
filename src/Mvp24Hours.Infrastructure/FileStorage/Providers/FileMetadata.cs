//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.FileStorage.Contract;
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.FileStorage.Providers
{
    /// <summary>
    /// Default implementation of <see cref="IFileMetadata"/>.
    /// </summary>
    /// <remarks>
    /// This class provides a standard implementation of file metadata that can be used
    /// by all storage providers. It includes all required properties and supports
    /// custom properties for provider-specific metadata.
    /// </remarks>
    public class FileMetadata : IFileMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileMetadata"/> class.
        /// </summary>
        /// <param name="filePath">The full path of the file in the storage system.</param>
        /// <param name="name">The name of the file (without directory path).</param>
        /// <param name="size">The size of the file in bytes.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="createdAt">The date and time when the file was created (in UTC).</param>
        /// <param name="modifiedAt">The date and time when the file was last modified (in UTC).</param>
        /// <param name="eTag">The ETag (entity tag) of the file, if available.</param>
        /// <param name="customProperties">Custom properties/metadata associated with the file.</param>
        /// <exception cref="ArgumentNullException">Thrown when filePath or name is null or empty.</exception>
        public FileMetadata(
            string filePath,
            string name,
            long size,
            string? contentType = null,
            DateTimeOffset? createdAt = null,
            DateTimeOffset? modifiedAt = null,
            string? eTag = null,
            IDictionary<string, string>? customProperties = null)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name), "File name cannot be null or empty.");
            }

            FilePath = filePath;
            Name = name;
            Size = size;
            ContentType = contentType;
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
            ModifiedAt = modifiedAt ?? CreatedAt;
            ETag = eTag;
            CustomProperties = customProperties ?? new Dictionary<string, string>();
        }

        /// <inheritdoc/>
        public string FilePath { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public long Size { get; }

        /// <inheritdoc/>
        public string? ContentType { get; }

        /// <inheritdoc/>
        public DateTimeOffset CreatedAt { get; }

        /// <inheritdoc/>
        public DateTimeOffset ModifiedAt { get; }

        /// <inheritdoc/>
        public string? ETag { get; }

        /// <inheritdoc/>
        public IDictionary<string, string> CustomProperties { get; }
    }
}

